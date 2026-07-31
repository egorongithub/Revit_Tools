using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Autodesk.Revit.DB;
using SminexBimTools.Core;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Выбор параметров из проекта: список проектных и общих параметров
    /// документа с типом данных.
    /// </summary>
    public partial class ParameterPickerWindow : Window
    {
        private readonly List<ParameterItemVm> _allItems;

        public List<string> SelectedNames { get; } = new List<string>();

        public ParameterPickerWindow(Document document)
        {
            InitializeComponent();

            _allItems = ModelParameterAnalyzer.Collect(document).Values
                .Select(info => new ParameterItemVm
                {
                    Name = info.Name,
                    DataTypeLabel = ModelParameterAnalyzer.GetDataTypeLabel(info.DataType),
                    KindLabel = info.IsShared ? "Общий" : "Проектный"
                })
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            ParamsList.ItemsSource = _allItems;
            FilterBox.Focus();
        }

        private void FilterBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string filter = (FilterBox.Text ?? string.Empty).Trim();
            ParamsList.ItemsSource = filter.Length == 0
                ? _allItems
                : _allItems.Where(item =>
                    item.Name.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0).ToList();
        }

        private void ParamsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Accept();
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Accept();
        }

        private void Accept()
        {
            SelectedNames.Clear();
            foreach (object item in ParamsList.SelectedItems)
            {
                if (item is ParameterItemVm vm)
                    SelectedNames.Add(vm.Name);
            }

            if (SelectedNames.Count == 0)
                return;

            DialogResult = true;
        }
    }

    public class ParameterItemVm
    {
        public string Name { get; set; }
        public string DataTypeLabel { get; set; }
        public string KindLabel { get; set; }
    }
}
