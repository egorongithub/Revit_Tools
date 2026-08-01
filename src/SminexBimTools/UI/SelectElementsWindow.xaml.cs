using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using SminexBimTools.Core;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Выбор элементов в модели по группам результата:
    /// категориям, параметрам-источникам и пропущенным.
    /// </summary>
    public partial class SelectElementsWindow : Window
    {
        private readonly UIDocument _uidoc;
        private readonly List<CheckBox> _checkBoxes = new List<CheckBox>();

        public SelectElementsWindow(UIDocument uidoc, SummationResult result)
        {
            InitializeComponent();
            _uidoc = uidoc;

            if (result.Categories.Count > 0)
            {
                AddGroupBox("По категориям", result.Categories
                    .OrderByDescending(p => p.Value.Count)
                    .Select(p => Item(
                        string.Format("{0} — {1} шт", p.Key, p.Value.Count),
                        p.Value.ElementIds)));
            }

            if (result.ByParameter.Count > 0)
            {
                AddGroupBox("По параметрам-источникам", result.ByParameter
                    .OrderByDescending(p => p.Value.Count)
                    .Select(p => Item(
                        string.Format("{0} — {1} шт", p.Key, p.Value.Count),
                        p.Value.ElementIds)));
            }

            if (result.SkippedIds.Count > 0)
            {
                AddGroupBox("Прочее", new[]
                {
                    Item(string.Format("Пропущенные (параметр не найден) — {0} шт", result.SkippedIds.Count),
                        result.SkippedIds)
                });
            }
        }

        private KeyValuePair<string, List<ElementId>> Item(string title, List<ElementId> ids)
        {
            return new KeyValuePair<string, List<ElementId>>(title, ids);
        }

        private void AddGroupBox(string header, IEnumerable<KeyValuePair<string, List<ElementId>>> items)
        {
            var panel = new StackPanel();
            foreach (KeyValuePair<string, List<ElementId>> item in items)
            {
                var checkBox = new CheckBox
                {
                    Content = item.Key,
                    Tag = item.Value,
                    Margin = new Thickness(4, 4, 4, 2)
                };
                _checkBoxes.Add(checkBox);
                panel.Children.Add(checkBox);
            }

            GroupsPanel.Children.Add(new GroupBox
            {
                Header = header,
                Content = panel,
                Padding = new Thickness(6),
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            var ids = new HashSet<ElementId>();
            foreach (CheckBox checkBox in _checkBoxes)
            {
                if (checkBox.IsChecked == true && checkBox.Tag is List<ElementId> groupIds)
                {
                    foreach (ElementId id in groupIds)
                        ids.Add(id);
                }
            }

            if (ids.Count == 0)
            {
                MessageBox.Show(this,
                    "Не отмечена ни одна группа.",
                    "Sminex BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            _uidoc.Selection.SetElementIds(ids.ToList());
            DialogResult = true;
        }
    }
}
