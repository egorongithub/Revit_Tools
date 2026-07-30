using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using SminexBimTools.Settings;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Окно настроек: имена параметров для каждой проверки.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public PluginSettings Settings { get; private set; }

        public SettingsWindow(PluginSettings settings)
        {
            InitializeComponent();

            for (int i = 0; i <= 6; i++)
                DecimalsCombo.Items.Add(i);

            Settings = settings ?? PluginSettings.CreateDefault();
            LoadToUi(Settings);
        }

        private void LoadToUi(PluginSettings settings)
        {
            VolumeBox.Text = Join(settings.VolumeParameters);
            AreaBox.Text = Join(settings.AreaParameters);
            LengthBox.Text = Join(settings.LengthParameters);
            CountBox.Text = Join(settings.CountParameters);
            TypeParamsCheck.IsChecked = settings.SearchTypeParameters;
            RoundUpCheck.IsChecked = settings.RoundUp;
            DecimalsCombo.SelectedIndex = Math.Max(0, Math.Min(6, settings.DecimalPlaces));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var settings = new PluginSettings
            {
                VolumeParameters = Parse(VolumeBox.Text),
                AreaParameters = Parse(AreaBox.Text),
                LengthParameters = Parse(LengthBox.Text),
                CountParameters = Parse(CountBox.Text),
                SearchTypeParameters = TypeParamsCheck.IsChecked == true,
                RoundUp = RoundUpCheck.IsChecked == true,
                DecimalPlaces = DecimalsCombo.SelectedIndex < 0 ? 3 : DecimalsCombo.SelectedIndex
            };

            try
            {
                SettingsManager.Save(settings);
            }
            catch (Exception exception)
            {
                MessageBox.Show(this,
                    "Не удалось сохранить настройки:\n" + exception.Message,
                    "Sminex BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            Settings = settings;
            DialogResult = true;
        }

        private void Defaults_Click(object sender, RoutedEventArgs e)
        {
            LoadToUi(PluginSettings.CreateDefault());
        }

        private static string Join(List<string> values)
        {
            return values == null ? string.Empty : string.Join(", ", values);
        }

        private static List<string> Parse(string text)
        {
            return (text ?? string.Empty)
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => part.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
