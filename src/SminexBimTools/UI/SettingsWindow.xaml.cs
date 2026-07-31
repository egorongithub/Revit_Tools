using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;
using SminexBimTools.Core;
using SminexBimTools.Settings;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Окно настроек: правила поиска параметров для каждой проверки,
    /// порядок источников и анализ параметров модели.
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private static readonly string[] SourceLabels = { "Авто", "Экземпляр", "Тип" };

        private static readonly Dictionary<SearchStage, string> StageLabels = new Dictionary<SearchStage, string>
        {
            { SearchStage.Type, "Тип" },
            { SearchStage.Instance, "Экземпляр" },
            { SearchStage.System, "Системные" }
        };

        private readonly Document _document;

        private readonly ObservableCollection<RuleVm> _volumeRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _areaRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _lengthRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _countRules = new ObservableCollection<RuleVm>();

        public PluginSettings Settings { get; private set; }

        public SettingsWindow(PluginSettings settings, Document document)
        {
            InitializeComponent();

            _document = document;
            Settings = settings ?? PluginSettings.CreateDefault();

            for (int i = 0; i <= 6; i++)
                DecimalsCombo.Items.Add(i);

            VolumeGrid.ItemsSource = _volumeRules;
            AreaGrid.ItemsSource = _areaRules;
            LengthGrid.ItemsSource = _lengthRules;
            CountGrid.ItemsSource = _countRules;

            foreach (DataGrid grid in AllGrids())
                ((DataGridComboBoxColumn)grid.Columns[1]).ItemsSource = SourceLabels;

            if (_document == null)
            {
                PickButton.IsEnabled = false;
                AnalyzeButton.IsEnabled = false;
                PickButton.ToolTip = AnalyzeButton.ToolTip = "Нет открытого документа";
            }

            LoadToUi(Settings);
        }

        // ---------- загрузка/выгрузка ----------

        private void LoadToUi(PluginSettings settings)
        {
            FillRules(_volumeRules, settings.VolumeRules);
            FillRules(_areaRules, settings.AreaRules);
            FillRules(_lengthRules, settings.LengthRules);
            FillRules(_countRules, settings.CountRules);

            OrderList.Items.Clear();
            foreach (SearchStage stage in settings.SearchOrder)
                OrderList.Items.Add(StageLabels[stage]);

            RoundUpCheck.IsChecked = settings.RoundUp;
            GroupCategoryCheck.IsChecked = settings.GroupByCategory;
            GroupParameterCheck.IsChecked = settings.GroupByParameter;
            DecimalsCombo.SelectedIndex = Math.Max(0, Math.Min(6, settings.DecimalPlaces));
        }

        private static void FillRules(ObservableCollection<RuleVm> target, List<ParameterRule> rules)
        {
            target.Clear();
            if (rules == null)
                return;
            foreach (ParameterRule rule in rules)
            {
                if (rule != null)
                    target.Add(new RuleVm(rule.Name, rule.Source));
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataGrid grid in AllGrids())
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            var settings = new PluginSettings
            {
                VolumeRules = CollectRules(_volumeRules),
                AreaRules = CollectRules(_areaRules),
                LengthRules = CollectRules(_lengthRules),
                CountRules = CollectRules(_countRules),
                SearchOrder = CollectOrder(),
                RoundUp = RoundUpCheck.IsChecked == true,
                GroupByCategory = GroupCategoryCheck.IsChecked == true,
                GroupByParameter = GroupParameterCheck.IsChecked == true,
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

        private static List<ParameterRule> CollectRules(ObservableCollection<RuleVm> rules)
        {
            return rules
                .Where(vm => !string.IsNullOrWhiteSpace(vm.Name))
                .Select(vm => new ParameterRule(vm.Name.Trim(), vm.Source))
                .ToList();
        }

        private List<SearchStage> CollectOrder()
        {
            var order = new List<SearchStage>();
            foreach (object item in OrderList.Items)
            {
                string label = item as string;
                foreach (KeyValuePair<SearchStage, string> pair in StageLabels)
                {
                    if (pair.Value == label)
                    {
                        order.Add(pair.Key);
                        break;
                    }
                }
            }
            return order;
        }

        private void Defaults_Click(object sender, RoutedEventArgs e)
        {
            LoadToUi(PluginSettings.CreateDefault());
        }

        // ---------- операции со списком правил ----------

        private DataGrid CurrentGrid()
        {
            switch (Tabs.SelectedIndex)
            {
                case 0: return VolumeGrid;
                case 1: return AreaGrid;
                case 2: return LengthGrid;
                default: return CountGrid;
            }
        }

        private ObservableCollection<RuleVm> CurrentRules()
        {
            switch (Tabs.SelectedIndex)
            {
                case 0: return _volumeRules;
                case 1: return _areaRules;
                case 2: return _lengthRules;
                default: return _countRules;
            }
        }

        private MeasureKind CurrentKind()
        {
            switch (Tabs.SelectedIndex)
            {
                case 0: return MeasureKind.Volume;
                case 1: return MeasureKind.Area;
                case 2: return MeasureKind.Length;
                default: return MeasureKind.Count;
            }
        }

        private IEnumerable<DataGrid> AllGrids()
        {
            yield return VolumeGrid;
            yield return AreaGrid;
            yield return LengthGrid;
            yield return CountGrid;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            ObservableCollection<RuleVm> rules = CurrentRules();
            var vm = new RuleVm(string.Empty, ParameterSource.Auto);
            rules.Add(vm);

            DataGrid grid = CurrentGrid();
            grid.SelectedItem = vm;
            grid.ScrollIntoView(vm);
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentGrid().SelectedItem is RuleVm vm)
                CurrentRules().Remove(vm);
        }

        private void RuleUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRule(-1);
        }

        private void RuleDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRule(1);
        }

        private void MoveSelectedRule(int delta)
        {
            DataGrid grid = CurrentGrid();
            ObservableCollection<RuleVm> rules = CurrentRules();
            if (!(grid.SelectedItem is RuleVm vm))
                return;

            int index = rules.IndexOf(vm);
            int newIndex = index + delta;
            if (index < 0 || newIndex < 0 || newIndex >= rules.Count)
                return;

            rules.Move(index, newIndex);
            grid.SelectedItem = vm;
        }

        // ---------- порядок поиска ----------

        private void OrderUp_Click(object sender, RoutedEventArgs e)
        {
            MoveOrderItem(-1);
        }

        private void OrderDown_Click(object sender, RoutedEventArgs e)
        {
            MoveOrderItem(1);
        }

        private void MoveOrderItem(int delta)
        {
            int index = OrderList.SelectedIndex;
            int newIndex = index + delta;
            if (index < 0 || newIndex < 0 || newIndex >= OrderList.Items.Count)
                return;

            object item = OrderList.Items[index];
            OrderList.Items.RemoveAt(index);
            OrderList.Items.Insert(newIndex, item);
            OrderList.SelectedIndex = newIndex;
        }

        // ---------- аналитика модели ----------

        private void Analyze_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
                return;

            Dictionary<string, ProjectParameterInfo> map = ModelParameterAnalyzer.Collect(_document);

            EvaluateRules(_volumeRules, MeasureKind.Volume, map);
            EvaluateRules(_areaRules, MeasureKind.Area, map);
            EvaluateRules(_lengthRules, MeasureKind.Length, map);
            EvaluateRules(_countRules, MeasureKind.Count, map);
        }

        private static void EvaluateRules(ObservableCollection<RuleVm> rules, MeasureKind kind, Dictionary<string, ProjectParameterInfo> map)
        {
            foreach (RuleVm vm in rules)
                vm.Status = ModelParameterAnalyzer.Evaluate(vm.Name, kind, map);
        }

        private void Pick_Click(object sender, RoutedEventArgs e)
        {
            if (_document == null)
                return;

            var picker = new ParameterPickerWindow(_document) { Owner = this };
            if (picker.ShowDialog() != true)
                return;

            ObservableCollection<RuleVm> rules = CurrentRules();
            var existing = new HashSet<string>(
                rules.Select(r => (r.Name ?? string.Empty).Trim()),
                StringComparer.OrdinalIgnoreCase);

            Dictionary<string, ProjectParameterInfo> map = ModelParameterAnalyzer.Collect(_document);
            MeasureKind kind = CurrentKind();

            foreach (string name in picker.SelectedNames)
            {
                if (existing.Contains(name))
                    continue;
                rules.Add(new RuleVm(name, ParameterSource.Auto)
                {
                    Status = ModelParameterAnalyzer.Evaluate(name, kind, map)
                });
                existing.Add(name);
            }
        }
    }

    /// <summary>Строка таблицы правил.</summary>
    public class RuleVm : INotifyPropertyChanged
    {
        private string _name;
        private string _sourceLabel;
        private string _status;

        public RuleVm(string name, ParameterSource source)
        {
            _name = name ?? string.Empty;
            _sourceLabel = ToLabel(source);
            _status = string.Empty;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; OnPropertyChanged(nameof(Name)); }
        }

        public string SourceLabel
        {
            get { return _sourceLabel; }
            set { _sourceLabel = value; OnPropertyChanged(nameof(SourceLabel)); }
        }

        public string Status
        {
            get { return _status; }
            set { _status = value; OnPropertyChanged(nameof(Status)); }
        }

        public ParameterSource Source
        {
            get
            {
                switch (_sourceLabel)
                {
                    case "Экземпляр": return ParameterSource.Instance;
                    case "Тип": return ParameterSource.Type;
                    default: return ParameterSource.Auto;
                }
            }
        }

        private static string ToLabel(ParameterSource source)
        {
            switch (source)
            {
                case ParameterSource.Instance: return "Экземпляр";
                case ParameterSource.Type: return "Тип";
                default: return "Авто";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
