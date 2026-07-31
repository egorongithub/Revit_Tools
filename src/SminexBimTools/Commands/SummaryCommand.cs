using System;
using System.Collections.Generic;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SminexBimTools.Core;
using SminexBimTools.Settings;

namespace SminexBimTools.Commands
{
    /// <summary>
    /// Кнопка «Сводка» — объем, площадь, длина и количество
    /// по выделенным элементам одним окном.
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SummaryCommand : IExternalCommand
    {
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

                ICollection<ElementId> ids = SelectionHelper.GetTargets(uidoc);
                if (ids == null)
                    return Result.Cancelled;

                if (ids.Count == 0)
                {
                    TaskDialog.Show("Sminex BIM Tools", "Не выделено ни одного элемента.");
                    return Result.Cancelled;
                }

                PluginSettings settings = SettingsManager.Load();
                Document doc = uidoc.Document;

                var kinds = new[] { MeasureKind.Volume, MeasureKind.Area, MeasureKind.Length, MeasureKind.Count };
                var content = new StringBuilder();
                var expanded = new StringBuilder();

                foreach (MeasureKind kind in kinds)
                {
                    SummationResult result = MeasureEngine.Sum(doc, ids, kind, settings);

                    int decimals = kind == MeasureKind.Count ? 0 : settings.DecimalPlaces;
                    string totalText = NumberFormatter.Format(result.Total, decimals, settings.RoundUp);

                    string line;
                    if (kind == MeasureKind.Count)
                    {
                        line = string.Format("{0}: {1} {2}", kind.DisplayName(), totalText, kind.Unit());
                    }
                    else
                    {
                        line = string.Format("{0}: {1} {2} — учтено {3} из {4}",
                            kind.DisplayName(), totalText, kind.Unit(), result.Counted, result.TotalElements);
                        if (kind == MeasureKind.Length && result.Total > 0)
                            line += string.Format(" ({0} мм)", NumberFormatter.Format(result.Total * 1000, 0, settings.RoundUp));
                    }
                    content.AppendLine(line);

                    var notes = new List<string>();
                    if (result.Skipped.Count > 0)
                        notes.Add(string.Format("пропущено {0} элем.", result.Skipped.Count));
                    if (kind != MeasureKind.Count && result.RawCount > 0)
                        notes.Add(string.Format("⚠ безразмерных значений: {0}", result.RawCount));
                    if (notes.Count > 0)
                        expanded.AppendLine(string.Format("{0}: {1}", kind.DisplayName(), string.Join("; ", notes)));
                }

                var dialog = new TaskDialog("Sminex BIM Tools")
                {
                    TitleAutoPrefix = false,
                    MainInstruction = string.Format("Сводка: {0} элем.", ids.Count),
                    MainContent = content.ToString().TrimEnd(),
                    CommonButtons = TaskDialogCommonButtons.Close,
                    DefaultButton = TaskDialogResult.Close,
                    AllowCancellation = true
                };
                if (expanded.Length > 0)
                    dialog.ExpandedContent = expanded.ToString().TrimEnd();

                dialog.Show();
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
