using System;
using System.IO;
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
            settings.VolumeParameters = settings.VolumeParameters ?? new System.Collections.Generic.List<string>();
            settings.AreaParameters = settings.AreaParameters ?? new System.Collections.Generic.List<string>();
            settings.LengthParameters = settings.LengthParameters ?? new System.Collections.Generic.List<string>();
            settings.CountParameters = settings.CountParameters ?? new System.Collections.Generic.List<string>();
            settings.DecimalPlaces = Math.Max(0, Math.Min(6, settings.DecimalPlaces));
            return settings;
        }
    }
}
