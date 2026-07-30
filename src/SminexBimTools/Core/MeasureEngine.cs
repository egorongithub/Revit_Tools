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
        /// <summary>
        /// Суммирует значение измерения <paramref name="kind"/> по элементам <paramref name="elementIds"/>.
        /// Имена параметров берутся из настроек и проверяются по порядку — используется
        /// первый найденный параметр со значением.
        /// </summary>
        public static SummationResult Sum(Document document, ICollection<ElementId> elementIds, MeasureKind kind, PluginSettings settings)
        {
            var result = new SummationResult { TotalElements = elementIds.Count };
            IList<string> parameterNames = settings.GetParameters(kind);

            foreach (ElementId id in elementIds)
            {
                Element element = document.GetElement(id);
                if (element == null)
                    continue;

                Parameter parameter = FindParameter(element, parameterNames, kind, settings.SearchTypeParameters);
                double? value = parameter != null ? ExtractValue(parameter) : null;
                string usedName = parameter != null ? parameter.Definition.Name : null;

                // Для «Количества» элемент без параметра считается за 1 штуку.
                if (value == null && kind == MeasureKind.Count)
                {
                    value = 1;
                    usedName = "по числу элементов (1 шт за элемент)";
                }

                if (value == null)
                {
                    result.Skipped.Add(Describe(element));
                    continue;
                }

                result.Counted++;
                result.Total += value.Value;

                if (usedName != null)
                {
                    result.UsedParameters.TryGetValue(usedName, out int usedCount);
                    result.UsedParameters[usedName] = usedCount + 1;
                }

                string categoryName = element.Category != null ? element.Category.Name : "Без категории";
                if (!result.Categories.TryGetValue(categoryName, out CategoryTotal categoryTotal))
                {
                    categoryTotal = new CategoryTotal();
                    result.Categories[categoryName] = categoryTotal;
                }
                categoryTotal.Sum += value.Value;
                categoryTotal.Count++;
            }

            return result;
        }

        /// <summary>
        /// Ищет параметр по списку имен: сначала в экземпляре, затем (опционально) в типе,
        /// затем пробует системный параметр Revit как запасной вариант.
        /// </summary>
        private static Parameter FindParameter(Element element, IList<string> names, MeasureKind kind, bool searchTypeParameters)
        {
            Parameter parameter = FindByNames(element, names);
            if (parameter != null)
                return parameter;

            if (searchTypeParameters)
            {
                ElementId typeId = element.GetTypeId();
                if (typeId != null && typeId != ElementId.InvalidElementId)
                {
                    Element typeElement = element.Document.GetElement(typeId);
                    if (typeElement != null)
                    {
                        parameter = FindByNames(typeElement, names);
                        if (parameter != null)
                            return parameter;
                    }
                }
            }

            BuiltInParameter builtIn = kind.FallbackBuiltInParameter();
            if (builtIn != BuiltInParameter.INVALID)
            {
                Parameter fallback = element.get_Parameter(builtIn);
                if (fallback != null && fallback.HasValue)
                    return fallback;
            }

            return null;
        }

        private static Parameter FindByNames(Element element, IList<string> names)
        {
            foreach (string rawName in names)
            {
                string name = rawName == null ? null : rawName.Trim();
                if (string.IsNullOrEmpty(name))
                    continue;

                Parameter parameter = element.LookupParameter(name);
                if (parameter != null && parameter.HasValue)
                    return parameter;
            }

            return null;
        }

        /// <summary>
        /// Читает значение параметра. Числа с физическим смыслом переводятся
        /// из внутренних единиц Revit в метрические (м, м², м³).
        /// </summary>
        private static double? ExtractValue(Parameter parameter)
        {
            switch (parameter.StorageType)
            {
                case StorageType.Double:
                    return ConvertFromInternal(parameter.AsDouble(), parameter);

                case StorageType.Integer:
                    return parameter.AsInteger();

                case StorageType.String:
                    string text = parameter.AsString();
                    if (string.IsNullOrWhiteSpace(text))
                        return null;
                    text = text.Replace(" ", string.Empty)
                               .Replace(" ", string.Empty)
                               .Replace(',', '.');
                    return double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsed)
                        ? parsed
                        : (double?)null;

                default:
                    return null;
            }
        }

        private static double ConvertFromInternal(double value, Parameter parameter)
        {
            ForgeTypeId dataType = parameter.Definition.GetDataType();

            if (dataType == SpecTypeId.Volume)
                return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.CubicMeters);
            if (dataType == SpecTypeId.Area)
                return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.SquareMeters);
            if (dataType == SpecTypeId.Length)
                return UnitUtils.ConvertFromInternalUnits(value, UnitTypeId.Meters);

            // Безразмерные значения (число, целое и т.п.) возвращаем как есть.
            return value;
        }

        private static string Describe(Element element)
        {
            string category = element.Category != null ? element.Category.Name : "Без категории";
            string name = string.IsNullOrEmpty(element.Name) ? "<без имени>" : element.Name;
            return string.Format("{0} — {1} [id {2}]", category, name, element.Id);
        }
    }
}
