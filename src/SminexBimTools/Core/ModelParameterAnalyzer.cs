using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SminexBimTools.Core
{
    /// <summary>Сведения об общем/проектном параметре, определенном в документе.</summary>
    public class ProjectParameterInfo
    {
        public string Name { get; set; }
#if REVIT2020 || REVIT2021
        public ParameterType DataType { get; set; }
#else
        public ForgeTypeId DataType { get; set; }
#endif
        public bool IsShared { get; set; }
    }

    /// <summary>
    /// Аналитика параметров модели: какие параметры определены в проекте,
    /// подходят ли они по типу данных для конкретной проверки.
    /// </summary>
    public static class ModelParameterAnalyzer
    {
        /// <summary>
        /// Собирает все проектные и общие параметры документа
        /// (параметры, определенные внутри семейств, сюда не попадают).
        /// </summary>
        public static Dictionary<string, ProjectParameterInfo> Collect(Document document)
        {
            var map = new Dictionary<string, ProjectParameterInfo>(StringComparer.OrdinalIgnoreCase);
            if (document == null)
                return map;

            var collector = new FilteredElementCollector(document).OfClass(typeof(ParameterElement));
            foreach (ParameterElement parameterElement in collector)
            {
                Definition definition = parameterElement.GetDefinition();
                if (definition == null || string.IsNullOrEmpty(definition.Name))
                    continue;

                map[definition.Name] = new ProjectParameterInfo
                {
                    Name = definition.Name,
#if REVIT2020 || REVIT2021
#pragma warning disable CS0618
                    DataType = definition.ParameterType,
#pragma warning restore CS0618
#else
                    DataType = definition.GetDataType(),
#endif
                    IsShared = parameterElement is SharedParameterElement
                };
            }

            return map;
        }

        /// <summary>Человекочитаемое имя типа данных параметра.</summary>
#if REVIT2020 || REVIT2021
        public static string GetDataTypeLabel(ParameterType dataType)
        {
            try
            {
#pragma warning disable CS0618
                return LabelUtils.GetLabelFor(dataType);
#pragma warning restore CS0618
            }
            catch
            {
                return dataType.ToString();
            }
        }
#else
        public static string GetDataTypeLabel(ForgeTypeId dataType)
        {
            if (dataType == null)
                return "?";

            try
            {
                return LabelUtils.GetLabelForSpec(dataType);
            }
            catch
            {
                return dataType.TypeId;
            }
        }
#endif

        /// <summary>
        /// Оценивает имя параметра для проверки <paramref name="kind"/>:
        /// найден ли он в проекте и допустим ли его тип данных.
        /// </summary>
        public static string Evaluate(string name, MeasureKind kind, Dictionary<string, ProjectParameterInfo> projectParameters)
        {
            name = name == null ? null : name.Trim();
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            // Совпадение с локализованным именем системного параметра этой проверки.
            BuiltInParameter builtIn = kind.FallbackBuiltInParameter();
            if (builtIn != BuiltInParameter.INVALID)
            {
                try
                {
                    if (string.Equals(LabelUtils.GetLabelFor(builtIn), name, StringComparison.OrdinalIgnoreCase))
                        return "✓ системный параметр Revit";
                }
                catch
                {
                    // Нет метки — пропускаем проверку на системный.
                }
            }

            if (!projectParameters.TryGetValue(name, out ProjectParameterInfo info))
                return "— не найден среди параметров проекта";

#if REVIT2020 || REVIT2021
            ParameterType dataType = info.DataType;
            ParameterType expected = ExpectedType(kind);
            bool typeMatches = expected != ParameterType.Invalid && dataType == expected;
            bool isNumberLike = dataType == ParameterType.Number || dataType == ParameterType.Integer;
            bool isText = dataType == ParameterType.Text;
#else
            ForgeTypeId dataType = info.DataType;
            ForgeTypeId expected = ExpectedSpec(kind);
            bool typeMatches = expected != null && dataType == expected;
            bool isNumberLike = dataType == SpecTypeId.Number || dataType == SpecTypeId.Int.Integer;
            bool isText = dataType == SpecTypeId.String.Text;
#endif

            if (typeMatches)
                return "✓ найден: " + GetDataTypeLabel(dataType);
            if (isNumberLike)
                return "• найден, без размерности — значение будет взято как есть";
            if (isText)
                return "• найден, текст — будет разобран как число";

            return "✗ тип данных не подходит: " + GetDataTypeLabel(dataType);
        }

#if REVIT2020 || REVIT2021
        private static ParameterType ExpectedType(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return ParameterType.Volume;
                case MeasureKind.Area: return ParameterType.Area;
                case MeasureKind.Length: return ParameterType.Length;
                default: return ParameterType.Invalid;
            }
        }
#else
        private static ForgeTypeId ExpectedSpec(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return SpecTypeId.Volume;
                case MeasureKind.Area: return SpecTypeId.Area;
                case MeasureKind.Length: return SpecTypeId.Length;
                default: return null;
            }
        }
#endif
    }
}
