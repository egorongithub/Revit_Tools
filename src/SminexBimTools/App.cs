using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using SminexBimTools.Commands;

namespace SminexBimTools
{
    /// <summary>
    /// Точка входа плагина: создаёт вкладку «Sminex BIM Tools» с кнопками
    /// Объем, Площадь, Длина, Количество и Настройки.
    /// </summary>
    public class App : IExternalApplication
    {
        public const string TabName = "Sminex BIM Tools";

        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                application.CreateRibbonTab(TabName);
            }
            catch (Autodesk.Revit.Exceptions.ArgumentException)
            {
                // Вкладка уже существует.
            }

            RibbonPanel sumPanel = GetOrCreatePanel(application, "Суммирование");
            RibbonPanel servicePanel = GetOrCreatePanel(application, "Сервис");

            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            AddButton(sumPanel, assemblyPath,
                "SBT_Volume", "Объем", typeof(VolumeCommand), "Volume",
                "Сумма объёмов выделенных элементов",
                "Суммирует значения параметра «Объем» (или параметров, заданных в Настройках) " +
                "по всем выделенным элементам и выводит результат в м³.",
                typeof(DocumentRequiredAvailability));

            AddButton(sumPanel, assemblyPath,
                "SBT_Area", "Площадь", typeof(AreaCommand), "Area",
                "Сумма площадей выделенных элементов",
                "Суммирует значения параметра «Площадь» (или параметров, заданных в Настройках) " +
                "по всем выделенным элементам и выводит результат в м².",
                typeof(DocumentRequiredAvailability));

            AddButton(sumPanel, assemblyPath,
                "SBT_Length", "Длина", typeof(LengthCommand), "Length",
                "Сумма длин выделенных элементов",
                "Суммирует значения параметра «Длина» (или параметров, заданных в Настройках) " +
                "по всем выделенным элементам и выводит результат в метрах.",
                typeof(DocumentRequiredAvailability));

            AddButton(sumPanel, assemblyPath,
                "SBT_Count", "Количество", typeof(CountCommand), "Count",
                "Количество выделенных элементов",
                "Показывает число выделенных элементов с разбивкой по категориям.",
                typeof(DocumentRequiredAvailability));

            AddButton(sumPanel, assemblyPath,
                "SBT_Summary", "Сводка", typeof(SummaryCommand), "Summary",
                "Объем, площадь, длина и количество одним окном",
                "Считает сразу все суммы («Объем», «Площадь», «Длина», «Количество») " +
                "по выделенным элементам и выводит их в одном окне.",
                typeof(DocumentRequiredAvailability));

            AddButton(servicePanel, assemblyPath,
                "SBT_Settings", "Настройки", typeof(SettingsCommand), "Settings",
                "Настройки Sminex BIM Tools",
                "Задает, какие параметры искать у элементов для проверок «Объем», " +
                "«Площадь», «Длина» и «Количество».",
                typeof(AlwaysAvailable));

            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private static RibbonPanel GetOrCreatePanel(UIControlledApplication application, string panelName)
        {
            foreach (RibbonPanel panel in application.GetRibbonPanels(TabName))
            {
                if (panel.Name == panelName)
                    return panel;
            }

            return application.CreateRibbonPanel(TabName, panelName);
        }

        private static void AddButton(
            RibbonPanel panel,
            string assemblyPath,
            string internalName,
            string text,
            Type commandType,
            string iconBaseName,
            string toolTip,
            string longDescription,
            Type availabilityType)
        {
            var data = new PushButtonData(internalName, text, assemblyPath, commandType.FullName)
            {
                ToolTip = toolTip,
                LongDescription = longDescription
            };

            BitmapImage smallIcon = LoadIcon(iconBaseName + "16.png");
            BitmapImage largeIcon = LoadIcon(iconBaseName + "32.png");
            if (smallIcon != null) data.Image = smallIcon;
            if (largeIcon != null) data.LargeImage = largeIcon;

            if (availabilityType != null)
                data.AvailabilityClassName = availabilityType.FullName;

            panel.AddItem(data);
        }

        private static BitmapImage LoadIcon(string fileName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                string resourceName = "SminexBimTools.Resources.Icons." + fileName;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                        return null;

                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                // Кнопка без иконки лучше, чем плагин, не загрузившийся из-за иконки.
                return null;
            }
        }
    }
}
