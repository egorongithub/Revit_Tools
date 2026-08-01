using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using Autodesk.Revit.UI;
using SminexBimTools.Settings;
using SminexBimTools.UI;

namespace SminexBimTools.Core
{
    /// <summary>
    /// Вывод результата суммирования пользователю.
    /// </summary>
    public static class ResultPresenter
    {
        private const int MaxSkippedLines = 15;

        public static void Show(UIDocument uidoc, MeasureKind kind, SummationResult result, PluginSettings settings)
        {
            string unit = kind.Unit();
            int decimals = kind == MeasureKind.Count ? 0 : settings.DecimalPlaces;
            string totalText = NumberFormatter.Format(result.Total, ResolveDecimals(result.Total, decimals), settings.RoundUp);

            var dialog = new TaskDialog("Sminex BIM Tools")
            {
                TitleAutoPrefix = false,
                MainInstruction = string.Format("{0}: {1} {2}", kind.SummaryTitle(), totalText, unit),
                CommonButtons = TaskDialogCommonButtons.Close,
                DefaultButton = TaskDialogResult.Close,
                AllowCancellation = true
            };

            var content = new StringBuilder();
            content.AppendLine(string.Format("Учтено элементов: {0} из {1}.", result.Counted, result.TotalElements));
            if (result.Skipped.Count > 0)
                content.AppendLine(string.Format("Пропущено (параметр не найден): {0}.", result.Skipped.Count));
            if (kind == MeasureKind.Length && result.Total > 0)
                content.AppendLine(string.Format("То же в миллиметрах: {0} мм.", NumberFormatter.Format(result.Total * 1000, 0, settings.RoundUp)));
            if (kind != MeasureKind.Count && result.RawCount > 0)
                content.AppendLine(string.Format(
                    "⚠ У {0} элем. значение взято из безразмерного параметра (число/текст) и просуммировано как есть — проверьте единицы.",
                    result.RawCount));
            dialog.MainContent = content.ToString().TrimEnd();

            dialog.ExpandedContent = BuildExpandedContent(result, unit, settings);

            bool canSelect = uidoc != null
                && (result.Categories.Count > 0 || result.ByParameter.Count > 0 || result.SkippedIds.Count > 0);
            if (canSelect)
            {
                dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1,
                    "Выбрать элементы в модели…",
                    "Выделить элементы по категориям, параметрам-источникам или пропущенные");
            }

            TaskDialogResult dialogResult = dialog.Show();

            if (canSelect && dialogResult == TaskDialogResult.CommandLink1)
            {
                var window = new SelectElementsWindow(uidoc, result);
                var helper = new WindowInteropHelper(window)
                {
                    Owner = uidoc.Application.MainWindowHandle
                };
                window.ShowDialog();
            }
        }

        private static string BuildExpandedContent(SummationResult result, string unit, PluginSettings settings)
        {
            var expanded = new StringBuilder();

            if (settings.GroupByCategory && result.Categories.Count > 0)
            {
                expanded.AppendLine("По категориям:");
                foreach (KeyValuePair<string, CategoryTotal> pair in result.Categories.OrderByDescending(p => p.Value.Sum))
                {
                    expanded.AppendLine(string.Format("    • {0}: {1} {2} ({3} шт)",
                        pair.Key,
                        NumberFormatter.Format(pair.Value.Sum, settings.DecimalPlaces, settings.RoundUp),
                        unit,
                        pair.Value.Count));
                }
            }

            if (settings.GroupByParameter && result.ByParameter.Count > 0)
            {
                expanded.AppendLine("По параметрам-источникам:");
                foreach (KeyValuePair<string, CategoryTotal> pair in result.ByParameter.OrderByDescending(p => p.Value.Sum))
                {
                    expanded.AppendLine(string.Format("    • {0}: {1} ({2} шт)",
                        pair.Key,
                        NumberFormatter.Format(pair.Value.Sum, settings.DecimalPlaces, settings.RoundUp),
                        pair.Value.Count));
                }
            }

            if (result.Skipped.Count > 0)
            {
                expanded.AppendLine("Пропущенные элементы:");
                foreach (string line in result.Skipped.Take(MaxSkippedLines))
                    expanded.AppendLine("    • " + line);
                if (result.Skipped.Count > MaxSkippedLines)
                    expanded.AppendLine(string.Format("    …и еще {0}.", result.Skipped.Count - MaxSkippedLines));
            }

            return expanded.Length > 0 ? expanded.ToString().TrimEnd() : null;
        }

        /// <summary>
        /// При нуле знаков после запятой дробный итог показывается
        /// с двумя знаками, чтобы не потерять информацию.
        /// </summary>
        private static int ResolveDecimals(double total, int decimals)
        {
            if (decimals > 0)
                return decimals;
            return total % 1 == 0 ? 0 : 2;
        }
    }
}
