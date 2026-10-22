using System;
using System.Collections.Generic;
using System.Threading;
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
        // Карта строится один раз за сессию Revit: перебор нескольких тысяч
        // значений enum, значительная часть которых бросает исключение
        // в LabelUtils (нет метки). Это заметная, но однократная задержка
        // при первом использовании шага «Системные»; Lazy гарантирует
        // единственную сборку даже при параллельном первом обращении.
        private static readonly Lazy<Dictionary<string, List<BuiltInParameter>>> ByLabel =
            new Lazy<Dictionary<string, List<BuiltInParameter>>>(Build, LazyThreadSafetyMode.ExecutionAndPublication);

        private static Dictionary<string, List<BuiltInParameter>> Build()
        {
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

            return map;
        }

        /// <summary>Существует ли системный параметр Revit с таким именем.</summary>
        public static bool ContainsName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && ByLabel.Value.ContainsKey(name.Trim());
        }

        /// <summary>
        /// Системный параметр элемента с данным именем и значением. Одно имя
        /// может принадлежать нескольким системным параметрам (например,
        /// «Ширина» у окон и воздуховодов — разные параметры), поэтому при
        /// неоднозначности предпочитается кандидат, чье измерение совпадает
        /// с измерением проверки; если такого нет — первый со значением.
        /// </summary>
        public static Parameter Find(Element element, string name, ValueDimension preferredDimension)
        {
            if (element == null || string.IsNullOrWhiteSpace(name))
                return null;
            if (!ByLabel.Value.TryGetValue(name.Trim(), out List<BuiltInParameter> candidates))
                return null;

            Parameter fallback = null;
            foreach (BuiltInParameter builtIn in candidates)
            {
                Parameter parameter = element.get_Parameter(builtIn);
                if (parameter == null || !parameter.HasValue)
                    continue;

                if (preferredDimension == ValueDimension.None
                    || UnitClassifier.ClassifyParameter(parameter) == preferredDimension)
                    return parameter;

                if (fallback == null)
                    fallback = parameter;
            }

            return fallback;
        }
    }
}
