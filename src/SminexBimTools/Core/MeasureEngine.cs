using System.Collections.Generic;
using System.Globalization;
using Autodesk.Revit.DB;
using SminexBimTools.Settings;

namespace SminexBimTools.Core
{
    /// <summary>
    /// Ядро плагина: поиск параметров у элементов и суммирование значений.
    /// </summary>
    public static class MeasureEngine
    {
        private class ParameterHit
        {
            public Parameter Parameter;
            public string SourceLabel;  // экземпляр / тип / системный
            public ParameterRule Rule;  // null для системного шага
        }

        /// <summary>
        /// Суммирует значение измерения <paramref name="kind"/> по элементам.
        /// Источники обходятся в порядке settings.SearchOrder; правила с явным
        /// источником («экземпляр»/«тип») проверяются только на своем шаге,
        /// правила «Авто» — на каждом. Используется первый найденный параметр
        /// со значением.
        /// </summary>
        public static SummationResult Sum(Document document, ICollection<ElementId> elementIds, MeasureKind kind, PluginSettings settings)
        {
            // «Количество» — простой счетчик выделенных элементов,
            // параметры для него не ищутся.
            if (kind == MeasureKind.Count)
                return CountElements(document, elementIds);

            var result = new SummationResult { TotalElements = elementIds.Count };
            IList<ParameterRule> rules = settings.GetRules(kind);

            foreach (ElementId id in elementIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                    continue;

                ParameterHit hit = Find(element, rules, kind, settings.SearchOrder);

                double? value = null;
                string unitLabel = null; // null => значение без размерности
                if (hit != null)
                    value = ExtractValue(hit.Parameter, out unitLabel);

                // Безразмерное значение с явно заданной единицей — переводим в м/м²/м³.
                if (value != null && unitLabel == null && hit.Rule != null && hit.Rule.Unit != RawUnit.Auto)
                {
                    value *= RawUnits.Factor(hit.Rule.Unit);
                    unitLabel = "число в " + RawUnits.Label(hit.Rule.Unit);
                }

                if (value == null)
                {
                    result.Skipped.Add(Describe(element));
                    result.SkippedIds.Add(element.Id);
                    continue;
                }

                string groupKey = string.Format("{0} ({1}, {2})",
                    hit.Parameter.Definition.Name,
                    hit.SourceLabel,
                    unitLabel ?? "число — как есть");
                if (unitLabel == null)
                    result.RawCount++;

                result.Counted++;
                result.Total += value.Value;
                Accumulate(result.ByParameter, groupKey, value.Value, element.Id);

                string categoryName = element.Category != null ? element.Category.Name : "Без категории";
                Accumulate(result.Categories, categoryName, value.Value, element.Id);
            }

            return result;
        }

        private static void Accumulate(Dictionary<string, CategoryTotal> map, string key, double value, ElementId id)
        {
            if (!map.TryGetValue(key, out CategoryTotal total))
            {
                total = new CategoryTotal();
                map[key] = total;
            }

            total.Sum += value;
            total.Count++;
            total.ElementIds.Add(id);
        }

        private static SummationResult CountElements(Document document, ICollection<ElementId> elementIds)
        {
            var result = new SummationResult { TotalElements = elementIds.Count };

            foreach (ElementId id in elementIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                    continue;

                result.Counted++;
                result.Total += 1;

                string categoryName = element.Category != null ? element.Category.Name : "Без категории";
                Accumulate(result.Categories, categoryName, 1, element.Id);
            }

            return result;
        }

        private static ParameterHit Find(Element element, IList<ParameterRule> rules, MeasureKind kind, IList<SearchStage> order)
        {
            Element typeElement = null;
            ElementId typeId = element.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
                typeElement = element.Document.GetElement(typeId);

            foreach (SearchStage stage in order)
            {
                switch (stage)
                {
                    case SearchStage.Instance:
                    {
                        Parameter parameter = FindByRules(element, rules, ParameterSource.Instance, out ParameterRule rule);
                        if (parameter != null)
                            return new ParameterHit { Parameter = parameter, SourceLabel = "экземпляр", Rule = rule };
                        break;
                    }

                    case SearchStage.Type:
                    {
                        if (typeElement == null)
                            break;
                        Parameter parameter = FindByRules(typeElement, rules, ParameterSource.Type, out ParameterRule rule);
                        if (parameter != null)
                            return new ParameterHit { Parameter = parameter, SourceLabel = "тип", Rule = rule };
                        break;
                    }

                    case SearchStage.System:
                    {
                        BuiltInParameter builtIn = kind.FallbackBuiltInParameter();
                        if (builtIn == BuiltInParameter.INVALID)
                            break;
                        Parameter parameter = element.get_Parameter(builtIn);
                        if (parameter != null && parameter.HasValue)
                            return new ParameterHit { Parameter = parameter, SourceLabel = "системный", Rule = null };
                        break;
                    }
                }
            }

            return null;
        }

        private static Parameter FindByRules(Element target, IList<ParameterRule> rules, ParameterSource stageSource, out ParameterRule matchedRule)
        {
            foreach (ParameterRule rule in rules)
            {
                if (rule == null)
                    continue;

                // Правило с явным источником срабатывает только на своем шаге.
                if (rule.Source != ParameterSource.Auto && rule.Source != stageSource)
                    continue;

                string name = rule.Name == null ? null : rule.Name.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                // У элемента может быть несколько параметров с одинаковым именем
                // (например, общий параметр и параметр семейства) — перебираем
                // все одноименные и берем первый со значением.
                foreach (Parameter parameter in target.GetParameters(name))
                {
                    if (parameter != null && parameter.HasValue)
                    {
                        matchedRule = rule;
                        return parameter;
                    }
                }
            }

            matchedRule = null;
            return null;
        }

        /// <summary>
        /// Читает значение параметра. Для чисел с физическим смыслом (длина,
        /// площадь, объем) значение переводится из внутренних единиц Revit
        /// в метрические, а <paramref name="unitLabel"/> получает единицу
        /// («м», «м²», «м³»). Для безразмерных значений (число, целое, текст)
        /// unitLabel остается null, значение берется как есть.
        /// </summary>
        private static double? ExtractValue(Parameter parameter, out string unitLabel)
        {
            unitLabel = null;

            switch (parameter.StorageType)
            {
                case StorageType.Double:
                {
                    double raw = parameter.AsDouble();
                    ForgeTypeId dataType = parameter.Definition.GetDataType();

                    if (dataType == SpecTypeId.Volume)
                    {
                        unitLabel = "м³";
                        return UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.CubicMeters);
                    }
                    if (dataType == SpecTypeId.Area)
                    {
                        unitLabel = "м²";
                        return UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.SquareMeters);
                    }
                    if (dataType == SpecTypeId.Length)
                    {
                        unitLabel = "м";
                        return UnitUtils.ConvertFromInternalUnits(raw, UnitTypeId.Meters);
                    }

                    return raw;
                }

                case StorageType.Integer:
                    return parameter.AsInteger();

                case StorageType.String:
                {
                    string text = parameter.AsString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;
                    text = text.Replace(" ", string.Empty)
                               .Replace(" ", string.Empty)
                               .Replace(',', '.');
                    return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
                        ? parsed
                        : (double?)null;
                }

                default:
                    return null;
            }
        }

        private static string Describe(Element element)
        {
            string category = element.Category != null ? element.Category.Name : "Без категории";
            string name = string.IsNullOrEmpty(element.Name) ? "<без имени>" : element.Name;
            return string.Format("{0} — {1} [id {2}]", category, name, element.Id);
        }
    }
}
