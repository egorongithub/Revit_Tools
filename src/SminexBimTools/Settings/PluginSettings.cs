using System.Collections.Generic;
using System.Xml.Serialization;
using SminexBimTools.Core;

namespace SminexBimTools.Settings
{
    /// <summary>
    /// Настройки плагина: какие параметры искать у элементов для каждой проверки.
    /// Имена проверяются по порядку — используется первый найденный параметр со значением.
    /// </summary>
    [XmlRoot("SminexBimToolsSettings")]
    public class PluginSettings
    {
        /// <summary>Имена параметров для проверки «Объем».</summary>
        public List<string> VolumeParameters { get; set; } = new List<string>();

        /// <summary>Имена параметров для проверки «Площадь».</summary>
        public List<string> AreaParameters { get; set; } = new List<string>();

        /// <summary>Имена параметров для проверки «Длина».</summary>
        public List<string> LengthParameters { get; set; } = new List<string>();

        /// <summary>Имена параметров для проверки «Количество».</summary>
        public List<string> CountParameters { get; set; } = new List<string>();

        /// <summary>Искать параметры также в типе элемента, если в экземпляре не найдено.</summary>
        public bool SearchTypeParameters { get; set; } = true;

        /// <summary>Число знаков после запятой в результатах.</summary>
        public int DecimalPlaces { get; set; } = 3;

        /// <summary>
        /// Округлять последнюю цифру вверх, как это делает Revit (0,8325 → 0,833).
        /// Если выключено, используется «банковское» округление к чётному (0,8325 → 0,832).
        /// </summary>
        public bool RoundUp { get; set; } = true;

        public static PluginSettings CreateDefault()
        {
            return new PluginSettings
            {
                VolumeParameters = new List<string> { "Объем", "Volume", "ADSK_Объем" },
                AreaParameters = new List<string> { "Площадь", "Area", "ADSK_Площадь" },
                LengthParameters = new List<string> { "Длина", "Length", "ADSK_Длина" },
                CountParameters = new List<string> { "Количество", "Count", "ADSK_Количество" },
                SearchTypeParameters = true,
                DecimalPlaces = 3,
                RoundUp = true
            };
        }

        public List<string> GetParameters(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return VolumeParameters ?? new List<string>();
                case MeasureKind.Area: return AreaParameters ?? new List<string>();
                case MeasureKind.Length: return LengthParameters ?? new List<string>();
                case MeasureKind.Count: return CountParameters ?? new List<string>();
                default: return new List<string>();
            }
        }
    }
}
