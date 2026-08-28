using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SminexBimTools.Settings
{
    /// <summary>
    /// Загрузка и сохранение настроек в профиле пользователя:
    /// %AppData%\Sminex\SminexBimTools\Settings.xml
    /// </summary>
    public static class SettingsManager
    {
        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Sminex", "SminexBimTools");

        private static readonly string SettingsPath = Path.Combine(SettingsDirectory, "Settings.xml");

        public static PluginSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var serializer = new XmlSerializer(typeof(PluginSettings));
                    using (var stream = File.OpenRead(SettingsPath))
                    {
                        if (serializer.Deserialize(stream) is PluginSettings settings)
                            return Sanitize(settings);
                    }
                }
            }
            catch
            {
                // Повреждённый файл настроек не должен ломать работу плагина —
                // молча возвращаемся к значениям по умолчанию.
            }

            return PluginSettings.CreateDefault();
        }

        public static void Save(PluginSettings settings)
        {
            Directory.CreateDirectory(SettingsDirectory);

            var serializer = new XmlSerializer(typeof(PluginSettings));
            using (var stream = File.Create(SettingsPath))
            {
                serializer.Serialize(stream, Sanitize(settings));
            }
        }

        private static PluginSettings Sanitize(PluginSettings settings)
        {
            settings.MigrateLegacyLists();

            settings.VolumeRules = settings.VolumeRules ?? new List<ParameterRule>();
            settings.AreaRules = settings.AreaRules ?? new List<ParameterRule>();
            settings.LengthRules = settings.LengthRules ?? new List<ParameterRule>();
            settings.MassRules = settings.MassRules ?? new List<ParameterRule>();
            settings.VolumeCategoryRules = settings.VolumeCategoryRules ?? new List<ParameterRule>();
            settings.AreaCategoryRules = settings.AreaCategoryRules ?? new List<ParameterRule>();
            settings.LengthCategoryRules = settings.LengthCategoryRules ?? new List<ParameterRule>();
            settings.MassCategoryRules = settings.MassCategoryRules ?? new List<ParameterRule>();
            settings.DecimalPlaces = Math.Max(0, Math.Min(6, settings.DecimalPlaces));

            // Скрытого системного шага больше нет — вычищаем его из порядка,
            // сохраняя относительный порядок Экземпляр/Тип (файлы старых версий).
            List<SearchStage> order = (settings.SearchOrder ?? new List<SearchStage>())
                .Where(stage => stage == SearchStage.Instance || stage == SearchStage.Type)
                .ToList();
            bool orderValid = order.Count == 2 && order[0] != order[1];
            settings.SearchOrder = orderValid ? order : PluginSettings.DefaultSearchOrder();

            MigrateToCurrentVersion(settings);

            return settings;
        }

        /// <summary>
        /// Однократно дозаполняет настройки, сохранённые старой версией плагина.
        /// Выполняется только при повышении версии схемы, поэтому правила,
        /// удалённые пользователем позже, повторно не добавляются.
        /// v2: правила массы и исключения по площади воздуховодов.
        /// v3: исключения по категориям переехали из общих списков
        ///     в отдельные (отдельная таблица в настройках).
        /// </summary>
        private static void MigrateToCurrentVersion(PluginSettings settings)
        {
            if (settings.Version >= PluginSettings.CurrentVersion)
                return;

            if (settings.Version < 2 && settings.MassRules.Count == 0)
                settings.MassRules.AddRange(PluginSettings.DefaultMassRules());

            // v3: правила с категорией больше не живут в общих списках —
            // переносим их в списки исключений, сохраняя порядок.
            MoveCategoryRules(settings.VolumeRules, settings.VolumeCategoryRules);
            MoveCategoryRules(settings.AreaRules, settings.AreaCategoryRules);
            MoveCategoryRules(settings.LengthRules, settings.LengthCategoryRules);
            MoveCategoryRules(settings.MassRules, settings.MassCategoryRules);

            if (settings.Version < 2)
            {
                foreach (ParameterRule candidate in PluginSettings.DefaultAreaCategoryRules())
                {
                    if (!ContainsRule(settings.AreaCategoryRules, candidate))
                        settings.AreaCategoryRules.Add(candidate);
                }
            }

            settings.Version = PluginSettings.CurrentVersion;
        }

        private static void MoveCategoryRules(List<ParameterRule> generalRules, List<ParameterRule> categoryRules)
        {
            foreach (ParameterRule rule in generalRules
                         .Where(r => r != null && !string.IsNullOrWhiteSpace(r.Category))
                         .ToList())
            {
                generalRules.Remove(rule);
                if (!ContainsRule(categoryRules, rule))
                    categoryRules.Add(rule);
            }
        }

        private static bool ContainsRule(List<ParameterRule> rules, ParameterRule candidate)
        {
            return rules.Any(rule =>
                rule != null
                && string.Equals((rule.Category ?? string.Empty).Trim(), (candidate.Category ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase)
                && string.Equals((rule.Name ?? string.Empty).Trim(), (candidate.Name ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }
}
