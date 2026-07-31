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
            settings.DecimalPlaces = Math.Max(0, Math.Min(6, settings.DecimalPlaces));

            // Порядок поиска должен содержать каждый из трех шагов ровно один раз.
            List<SearchStage> order = settings.SearchOrder;
            bool orderValid = order != null
                && order.Count == 3
                && order.Distinct().Count() == 3
                && order.Contains(SearchStage.Type)
                && order.Contains(SearchStage.Instance)
                && order.Contains(SearchStage.System);
            if (!orderValid)
            {
                settings.SearchOrder = new List<SearchStage>
                {
                    SearchStage.Type,
                    SearchStage.Instance,
                    SearchStage.System
                };
            }

            return settings;
        }
    }
}
