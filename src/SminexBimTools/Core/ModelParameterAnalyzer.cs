using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SminexBimTools.Core
{
    /// <summary>Сведения об общем/проектном параметре, определенном в документе.</summary>
    public class ProjectParameterInfo
    {
        public string Name { get; set; }
        public ForgeTypeId DataType { get; set; }
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
                    DataType = definition.GetDataType(),
                    IsShared = parameterElement is SharedParameterElement
                };
            }

            return map;
        }

        /// <summary>Человекочитаемое имя типа данных параметра.</summary>
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

            ForgeTypeId dataType = info.DataType;
            ForgeTypeId expected = ExpectedSpec(kind);
            bool isNumberLike = dataType == SpecTypeId.Number || dataType == SpecTypeId.Int.Integer;
            bool isText = dataType == SpecTypeId.String.Text;

            if (expected != null && dataType == expected)
                return "✓ найден: " + GetDataTypeLabel(dataType);
            if (kind == MeasureKind.Count && isNumberLike)
                return "✓ найден: " + GetDataTypeLabel(dataType);
            if (isNumberLike)
                return "• найден, без размерности — значение будет взято как есть";
            if (isText)
                return "• найден, текст — будет разобран как число";

            return "✗ тип данных не подходит: " + GetDataTypeLabel(dataType);
        }

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
    }
}
