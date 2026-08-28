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

        /// <summary>Правила для проверки «Масса».</summary>
        public List<ParameterRule> MassRules { get; set; } = new List<ParameterRule>();

        // Исключения по категориям: правила с заполненным Category.
        // Для элементов «своей» категории действуют только они — без общих
        // правил и без системного параметра Revit. Проверяются строго
        // сверху вниз (строка за строкой).

        /// <summary>Исключения по категориям для «Объема».</summary>
        public List<ParameterRule> VolumeCategoryRules { get; set; } = new List<ParameterRule>();

        /// <summary>Исключения по категориям для «Площади».</summary>
        public List<ParameterRule> AreaCategoryRules { get; set; } = new List<ParameterRule>();

        /// <summary>Исключения по категориям для «Длины».</summary>
        public List<ParameterRule> LengthCategoryRules { get; set; } = new List<ParameterRule>();

        /// <summary>Исключения по категориям для «Массы».</summary>
        public List<ParameterRule> MassCategoryRules { get; set; } = new List<ParameterRule>();

        /// <summary>
        /// Версия схемы настроек — для дозаполнения новых правил
        /// при обновлении плагина поверх старого файла настроек.
        /// </summary>
        [XmlAttribute]
        public int Version { get; set; }

        /// <summary>Текущая версия схемы настроек.</summary>
        public const int CurrentVersion = 3;

        /// <summary>
        /// Общий порядок поиска для правил с источником «Авто» и системного шага.
        /// По умолчанию: системные → экземпляр → тип.
        /// ВАЖНО: инициализатор обязан быть пустым — XmlSerializer при чтении
        /// не заменяет заполненные списки, а дописывает в них, из-за чего
        /// сохранённый порядок «портился» и сбрасывался на умолчание.
        /// </summary>
        public List<SearchStage> SearchOrder { get; set; } = new List<SearchStage>();

        public static List<SearchStage> DefaultSearchOrder()
        {
            return new List<SearchStage>
            {
                SearchStage.System,
                SearchStage.Instance,
                SearchStage.Type
            };
        }

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

        public static PluginSettings CreateDefault()
        {
            return new PluginSettings
            {
                Version = CurrentVersion,
                VolumeRules = MakeRules("Объем", "Volume", "ADSK_Объем"),
                AreaRules = MakeRules("Площадь", "Area", "ADSK_Площадь"),
                LengthRules = MakeRules("Длина", "Length", "ADSK_Длина"),
                MassRules = DefaultMassRules(),
                AreaCategoryRules = DefaultAreaCategoryRules(),
                SearchOrder = DefaultSearchOrder()
            };
        }

        /// <summary>Правила «Массы» по умолчанию: SMNX_Масса из экземпляра.</summary>
        public static List<ParameterRule> DefaultMassRules()
        {
            return new List<ParameterRule>
            {
                new ParameterRule("SMNX_Масса", ParameterSource.Instance)
            };
        }

        /// <summary>
        /// Преднастроенные исключения по категориям для «Площади»:
        /// соединительные детали и изоляция воздуховодов всегда считаются
        /// из SMNX_Площадь экземпляра (системная площадь не используется).
        /// </summary>
        public static List<ParameterRule> DefaultAreaCategoryRules()
        {
            return new List<ParameterRule>
            {
                new ParameterRule("SMNX_Площадь", ParameterSource.Instance)
                {
                    Category = "Соединительные детали воздуховодов"
                },
                new ParameterRule("SMNX_Площадь", ParameterSource.Instance)
                {
                    Category = "Материалы изоляции воздуховодов"
                }
            };
        }

        public List<ParameterRule> GetRules(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return VolumeRules ?? new List<ParameterRule>();
                case MeasureKind.Area: return AreaRules ?? new List<ParameterRule>();
                case MeasureKind.Length: return LengthRules ?? new List<ParameterRule>();
                case MeasureKind.Mass: return MassRules ?? new List<ParameterRule>();
                default: return new List<ParameterRule>();
            }
        }

        public List<ParameterRule> GetCategoryRules(MeasureKind kind)
        {
            switch (kind)
            {
                case MeasureKind.Volume: return VolumeCategoryRules ?? new List<ParameterRule>();
                case MeasureKind.Area: return AreaCategoryRules ?? new List<ParameterRule>();
                case MeasureKind.Length: return LengthCategoryRules ?? new List<ParameterRule>();
                case MeasureKind.Mass: return MassCategoryRules ?? new List<ParameterRule>();
                default: return new List<ParameterRule>();
            }
        }

        /// <summary>Перенос списков имен из старого формата настроек в правила.</summary>
        public void MigrateLegacyLists()
        {
            VolumeRules = MigrateOne(VolumeRules, LegacyVolumeParameters);
            AreaRules = MigrateOne(AreaRules, LegacyAreaParameters);
            LengthRules = MigrateOne(LengthRules, LegacyLengthParameters);

            LegacyVolumeParameters = null;
            LegacyAreaParameters = null;
            LegacyLengthParameters = null;
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
