using System;
using System.Windows.Interop;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SminexBimTools.Settings;
using SminexBimTools.UI;

namespace SminexBimTools.Commands
{
    /// <summary>Кнопка «Настройки» — окно настройки параметров для проверок.</summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                PluginSettings settings = SettingsManager.Load();

                var window = new SettingsWindow(settings);
                var helper = new WindowInteropHelper(window)
                {
                    Owner = commandData.Application.MainWindowHandle
                };

                window.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                message = exception.ToString();
                return Result.Failed;
            }
        }
    }
}
