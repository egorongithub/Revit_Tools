using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using SminexBimTools.Core;

namespace SminexBimTools.Settings
{
    /// <summary>
    /// Настройки плагина: какие параметры искать у элементов для каждой проверки
    /// и в каком порядке обходить источники (тип / экземпляр / системные).
    /// Правила проверяются по порядку — используется первый найденный параметр
    /// со значением.
    /// </summary>
    [XmlRoot("SminexBimToolsSettings")]
    public class PluginSettings
    {
        /// <summary>Правила для проверки «Объем».</summary>
        public List<ParameterRule> VolumeRules { get; set; } = new List<ParameterRule>();

        /// <summary>Правила для проверки «Площадь».</summary>
        public List<ParameterRule> AreaRules { get; set; } = new List<ParameterRule>();

        /// <summary>Правила для проверки «Длина».</summary>
        public List<ParameterRule> LengthRules { get; set; } = new List<ParameterRule>();

        /// <summary>Правила для проверки «Количество».</summary>
        public List<ParameterRule> CountRules { get; set; } = new List<ParameterRule>();

        /// <summary>
        /// Общий порядок поиска для правил с источником «Авто» и системного шага.
        /// По умолчанию: тип → экземпляр → системные.
        /// </summary>
        public List<SearchStage> SearchOrder { get; set; } = new List<SearchStage>
        {
            SearchStage.Type,
            SearchStage.Instance,
            SearchStage.System
        };

        /// <summary>Число знаков после запятой в результатах.</summary>
        public int DecimalPlaces { get; set; } = 3;

        /// <summary>
        /// Округлять последнюю цифру вверх, как это делает Revit (0,8325 → 0,833).
        /// Если выключено, используется «банковское» округление к чётному.
        /// </summary>
        public bool RoundUp { get; set; } = true;

        /// <summary>Показывать в результатах разбивку по категориям.</summary>
        public bool GroupByCategory { get; set; } = true;

        /// <summary>Показывать в результатах разбивку по параметрам-источникам.</summary>
        public bool GroupByParameter { get; set; } = true;

        // ---- Устаревшие поля: только для чтения старых файлов настроек ----

        [XmlArray("VolumeParameters"), XmlArrayItem("string")]
        public List<string> LegacyVolumeParameters { get; set; }

        [XmlArray("AreaParameters"), XmlArrayItem("string")]
        public List<string> LegacyAreaParameters { get; set; }

        [XmlArray("LengthParameters"), XmlArrayItem("string")]
        public List<string> LegacyLengthParameters { get; set; }

        [XmlArray("CountParameters"), XmlArrayItem("string")]
        public List<string> LegacyCountParameters { get; set; }

        public static PluginSettings CreateDefault()
        {
            return new PluginSettings
            {
                VolumeRules = MakeRules("Объем", "Volume", "ADSK_Объем"),
                AreaRules = MakeRules("Площадь", "Area", "ADSK_Площадь"),
                LengthRules = MakeRules("Длина", "Length", "ADSK_Длина"),
                CountRules = MakeRules("Количество", "Count", "ADSK_Количество")
            };
        }

        public List<ParameterRule> GetRules(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return VolumeRules ?? new List<ParameterRule>();
                case MeasureKind.Area: return AreaRules ?? new List<ParameterRule>();
                case MeasureKind.Length: return LengthRules ?? new List<ParameterRule>();
                case MeasureKind.Count: return CountRules ?? new List<ParameterRule>();
                default: return new List<ParameterRule>();
            }
        }

        /// <summary>Перенос списков имен из старого формата настроек в правила.</summary>
        public void MigrateLegacyLists()
        {
            VolumeRules = MigrateOne(VolumeRules, LegacyVolumeParameters);
            AreaRules = MigrateOne(AreaRules, LegacyAreaParameters);
            LengthRules = MigrateOne(LengthRules, LegacyLengthParameters);
            CountRules = MigrateOne(CountRules, LegacyCountParameters);

            LegacyVolumeParameters = null;
            LegacyAreaParameters = null;
            LegacyLengthParameters = null;
            LegacyCountParameters = null;
        }

        private static List<ParameterRule> MigrateOne(List<ParameterRule> rules, List<string> legacyNames)
        {
            if (rules != null && rules.Count > 0)
                return rules;
            if (legacyNames == null || legacyNames.Count == 0)
                return rules ?? new List<ParameterRule>();

            return legacyNames
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => new ParameterRule(name.Trim()))
                .ToList();
        }

        private static List<ParameterRule> MakeRules(params string[] names)
        {
            return names.Select(name => new ParameterRule(name)).ToList();
        }
    }
}
