using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
        /// Правила проверяются строго сверху вниз (построчно); источник строки
        /// задает место поиска, «Авто» обходит места в порядке
        /// settings.SearchOrder (экземпляр/тип/системные). Используется первый
        /// найденный параметр со значением.
        /// </summary>
        public static SummationResult Sum(Document document, ICollection<ElementId> elementIds, MeasureKind kind, PluginSettings settings)
        {
            // «Количество» — простой счетчик выделенных элементов,
            // параметры для него не ищутся.
            if (kind == MeasureKind.Count)
                return CountElements(document, elementIds);

            var result = new SummationResult { TotalElements = elementIds.Count };

            // Общие правила — без категории (строки с категорией в общем списке
            // игнорируются защитно: после миграции их там быть не должно).
            var generalRules = settings.GetRules(kind)
                .Where(rule => rule != null && string.IsNullOrWhiteSpace(rule.Category))
                .ToList();

            // Исключения: для элементов «своей» категории действуют ТОЛЬКО они —
            // без общих правил и без системного параметра Revit.
            var categoryRules = new Dictionary<string, List<ParameterRule>>(StringComparer.OrdinalIgnoreCase);
            foreach (ParameterRule rule in settings.GetCategoryRules(kind))
            {
                if (rule == null)
                    continue;

                string ruleCategory = rule.Category == null ? null : rule.Category.Trim();
                if (string.IsNullOrEmpty(ruleCategory))
                    continue;

                if (!categoryRules.TryGetValue(ruleCategory, out List<ParameterRule> list))
                {
                    list = new List<ParameterRule>();
                    categoryRules[ruleCategory] = list;
                }
                list.Add(rule);
            }

            foreach (ElementId id in elementIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                    continue;

                string categoryName = element.Category != null ? element.Category.Name : "Без категории";

                List<ParameterRule> activeRules =
                    categoryRules.TryGetValue(categoryName, out List<ParameterRule> overrides)
                        ? overrides
                        : generalRules;
                ParameterHit hit = FindRowMajor(element, activeRules, settings.SearchOrder);

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

        /// <summary>
        /// Поиск по правилам строго сверху вниз, строка за строкой: Параметр 1
        /// проверяется по своему источнику, затем Параметр 2 и т.д. Источник
        /// «Экземпляр» — только имя в экземпляре, «Тип» — только в типе,
        /// «Авто» — шаги общего порядка поиска по очереди: экземпляр, тип,
        /// системные. Шаг «Системные» находит встроенные параметры Revit
        /// по имени строки — включая те, которых нет в коллекции Parameters
        /// элемента и которые обычному поиску по имени недоступны.
        /// </summary>
        private static ParameterHit FindRowMajor(Element element, IList<ParameterRule> rules, IList<SearchStage> order)
        {
            Element typeElement = null;
            ElementId typeId = element.GetTypeId();
            if (typeId != null && typeId != ElementId.InvalidElementId)
                typeElement = element.Document.GetElement(typeId);

            foreach (ParameterRule rule in rules)
            {
                if (rule == null)
                    continue;

                string name = rule.Name == null ? null : rule.Name.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                switch (rule.Source)
                {
                    case ParameterSource.Instance:
                    {
                        Parameter parameter = FindByName(element, name);
                        if (parameter != null)
                            return MakeHit(parameter, "экземпляр", rule);
                        break;
                    }

                    case ParameterSource.Type:
                    {
                        Parameter parameter = typeElement != null ? FindByName(typeElement, name) : null;
                        if (parameter != null)
                            return MakeHit(parameter, "тип", rule);
                        break;
                    }

                    default: // Auto — места проверяются в последовательности общего порядка
                    {
                        foreach (SearchStage stage in order)
                        {
                            if (stage == SearchStage.Instance)
                            {
                                Parameter parameter = FindByName(element, name);
                                if (parameter != null)
                                    return MakeHit(parameter, "экземпляр", rule);
                            }
                            else if (stage == SearchStage.Type && typeElement != null)
                            {
                                Parameter parameter = FindByName(typeElement, name);
                                if (parameter != null)
                                    return MakeHit(parameter, "тип", rule);
                            }
                            else if (stage == SearchStage.System)
                            {
                                Parameter parameter = BuiltInParameterMap.Find(element, name);
                                if (parameter != null)
                                    return MakeHit(parameter, "системный", rule);
                            }
                        }
                        break;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Первый параметр элемента с данным именем и значением
        /// (одноименных параметров может быть несколько).
        /// </summary>
        private static Parameter FindByName(Element target, string name)
        {
            foreach (Parameter parameter in target.GetParameters(name))
            {
                if (parameter != null && parameter.HasValue)
                    return parameter;
            }

            return null;
        }

        /// <summary>
        /// Встроенные параметры Revit подписываются «системный», чтобы
        /// в результатах было видно происхождение значения.
        /// </summary>
        private static ParameterHit MakeHit(Parameter parameter, string locationLabel, ParameterRule rule)
        {
            var internalDefinition = parameter.Definition as InternalDefinition;
            bool isBuiltIn = internalDefinition != null
                && internalDefinition.BuiltInParameter != BuiltInParameter.INVALID;

            return new ParameterHit
            {
                Parameter = parameter,
                SourceLabel = isBuiltIn ? "системный" : locationLabel,
                Rule = rule
            };
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

                    // Измерение определяется по допустимым единицам типа данных —
                    // так конвертируются не только «Длина»/«Площадь»/«Объем»/«Масса»,
                    // но и специальные виды: «Размер воздуховода», «Размер трубы»,
                    // толщины изоляции и т.п.
#if REVIT2020 || REVIT2021
#pragma warning disable CS0618
                    ValueDimension dimension = UnitClassifier.Classify(parameter.Definition.UnitType);
#pragma warning restore CS0618
#else
                    ValueDimension dimension = UnitClassifier.Classify(parameter.Definition.GetDataType());
#endif
                    if (dimension != ValueDimension.None)
                    {
                        unitLabel = UnitClassifier.UnitLabel(dimension);
                        return UnitClassifier.ConvertFromInternal(raw, dimension);
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
