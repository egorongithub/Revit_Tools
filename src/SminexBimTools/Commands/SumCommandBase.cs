using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using SminexBimTools.Core;
using SminexBimTools.Settings;

namespace SminexBimTools.Commands
{
    /// <summary>
    /// Общая логика кнопок суммирования: берёт выделенные элементы
    /// (или предлагает выбрать их, если ничего не выделено), суммирует
    /// значение и показывает результат.
    /// </summary>
    public abstract class SumCommandBase : IExternalCommand
    {
        protected abstract MeasureKind Kind { get; }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                if (uidoc == null)
                {
                    message = "Нет активного документа.";
                    return Result.Failed;
                }

                Document doc = uidoc.Document;
                ICollection<ElementId> ids = uidoc.Selection.GetElementIds();

                if (ids == null || ids.Count == 0)
                {
                    try
                    {
                        IList<Reference> references = uidoc.Selection.PickObjects(
                            ObjectType.Element,
                            "Выберите элементы и нажмите «Готово»");
                        ids = references.Select(r => r.ElementId).ToList();
                    }
                    catch (Autodesk.Revit.Exceptions.OperationCanceledException)
                    {
                        return Result.Cancelled;
                    }
                }

                if (ids.Count == 0)
                {
                    TaskDialog.Show("Sminex BIM Tools", "Не выделено ни одного элемента.");
                    return Result.Cancelled;
                }

                PluginSettings settings = SettingsManager.Load();
                SummationResult result = MeasureEngine.Sum(doc, ids, Kind, settings);
                ResultPresenter.Show(Kind, result, settings);

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
