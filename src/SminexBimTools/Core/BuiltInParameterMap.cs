using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace SminexBimTools.Core
{
    /// <summary>
    /// Карта «имя → системные параметры Revit»: по локализованной метке
    /// находит значения BuiltInParameter. Нужна шагу «Системные»: часть
    /// встроенных параметров отсутствует в коллекции Parameters элемента
    /// и обычным поиском по имени не находится — только напрямую через
    /// get_Parameter(BuiltInParameter).
    /// </summary>
    public static class BuiltInParameterMap
    {
        private static Dictionary<string, List<BuiltInParameter>> _byLabel;

        private static Dictionary<string, List<BuiltInParameter>> ByLabel
        {
            get
            {
                if (_byLabel == null)
                {
                    // Строится один раз за сессию (несколько тысяч значений enum).
                    var map = new Dictionary<string, List<BuiltInParameter>>(StringComparer.OrdinalIgnoreCase);
                    foreach (BuiltInParameter builtIn in Enum.GetValues(typeof(BuiltInParameter)))
                    {
                        if (builtIn == BuiltInParameter.INVALID)
                            continue;

                        string label;
                        try
                        {
                            label = LabelUtils.GetLabelFor(builtIn);
                        }
                        catch
                        {
                            continue; // у многих значений enum метки нет
                        }

                        if (string.IsNullOrEmpty(label))
                            continue;

                        if (!map.TryGetValue(label, out List<BuiltInParameter> list))
                        {
                            list = new List<BuiltInParameter>();
                            map[label] = list;
                        }
                        list.Add(builtIn);
                    }

                    _byLabel = map;
                }

                return _byLabel;
            }
        }

        /// <summary>Существует ли системный параметр Revit с таким именем.</summary>
        public static bool ContainsName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && ByLabel.ContainsKey(name.Trim());
        }

        /// <summary>
        /// Первый системный параметр элемента с данным именем и значением.
        /// Одно имя может принадлежать нескольким системным параметрам
        /// (например, «Ширина» у окон и воздуховодов — разные параметры) —
        /// проверяются все кандидаты.
        /// </summary>
        public static Parameter Find(Element element, string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            if (!ByLabel.TryGetValue(name.Trim(), out List<BuiltInParameter> candidates))
                return null;

            foreach (BuiltInParameter builtIn in candidates)
            {
                Parameter parameter = element.get_Parameter(builtIn);
                if (parameter != null && parameter.HasValue)
                    return parameter;
            }

            return null;
        }
    }
}
