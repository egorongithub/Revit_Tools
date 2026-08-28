using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Autodesk.Revit.DB;
using SminexBimTools.Core;
using SminexBimTools.Settings;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Окно настроек: общие правила и исключения по категориям для каждой
    /// проверки, порядок источников и анализ параметров модели.
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

        private const string InfoText =
            "• Правила проверяются сверху вниз — используется первый найденный параметр со значением.\n\n" +
            "• Источник «Авто» ищется по общему порядку поиска (внизу окна); «Экземпляр» или «Тип» — только там.\n\n" +
            "• Исключения по категориям: если для категории элемента заданы правила-исключения, действуют только они — " +
            "общие правила и системный параметр Revit не используются, элемент без значения попадает в «пропущенные». " +
            "Правила исключений проверяются строго сверху вниз.\n\n" +
            "• Вид параметра не важен: по имени находится любой параметр — системный, общий, проекта или семейства. " +
            "Кнопка «Проверить в модели» видит только проектные и общие параметры документа; параметры семейств " +
            "при подсчете читаются, но проверке недоступны.\n\n" +
            "• «Размерность» — для безразмерных чисел и текста: значение трактуется в выбранной единице и переводится " +
            "в метрические. «Авто» — берется как есть.";

        private readonly Document _document;

        private readonly ObservableCollection<RuleVm> _volumeRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _areaRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _lengthRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _massRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _volumeCategoryRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _areaCategoryRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _lengthCategoryRules = new ObservableCollection<RuleVm>();
        private readonly ObservableCollection<RuleVm> _massCategoryRules = new ObservableCollection<RuleVm>();

        private Dictionary<string, DataGrid> _gridsByKey;
        private Dictionary<DataGrid, ObservableCollection<RuleVm>> _gridRules;
        private Dictionary<DataGrid, MeasureKind> _gridKinds;

        public PluginSettings Settings { get; private set; }

        public SettingsWindow(PluginSettings settings, Document document)
        {
            InitializeComponent();

            _document = document;
            Settings = settings ?? PluginSettings.CreateDefault();

            for (int i = 0; i <= 6; i++)
                DecimalsCombo.Items.Add(i);

            _gridsByKey = new Dictionary<string, DataGrid>
            {
                { "Volume", VolumeGrid },
                { "Area", AreaGrid },
                { "Length", LengthGrid },
                { "Mass", MassGrid },
                { "VolumeCategory", VolumeCategoryGrid },
                { "AreaCategory", AreaCategoryGrid },
                { "LengthCategory", LengthCategoryGrid },
                { "MassCategory", MassCategoryGrid }
            };

            _gridRules = new Dictionary<DataGrid, ObservableCollection<RuleVm>>
            {
                { VolumeGrid, _volumeRules },
                { AreaGrid, _areaRules },
                { LengthGrid, _lengthRules },
                { MassGrid, _massRules },
                { VolumeCategoryGrid, _volumeCategoryRules },
                { AreaCategoryGrid, _areaCategoryRules },
                { LengthCategoryGrid, _lengthCategoryRules },
                { MassCategoryGrid, _massCategoryRules }
            };

            _gridKinds = new Dictionary<DataGrid, MeasureKind>
            {
                { VolumeGrid, MeasureKind.Volume },
                { AreaGrid, MeasureKind.Area },
                { LengthGrid, MeasureKind.Length },
                { MassGrid, MeasureKind.Mass },
                { VolumeCategoryGrid, MeasureKind.Volume },
                { AreaCategoryGrid, MeasureKind.Area },
                { LengthCategoryGrid, MeasureKind.Length },
                { MassCategoryGrid, MeasureKind.Mass }
            };

            foreach (KeyValuePair<DataGrid, ObservableCollection<RuleVm>> pair in _gridRules)
                pair.Key.ItemsSource = pair.Value;

            // В общих таблицах комбобоксы — колонки 1 и 2, в таблицах
            // исключений (есть колонка «Категория») — колонки 2 и 3.
            SetupComboColumns(VolumeGrid, 1, new[] { "Авто", "мм³", "л", "м³" });
            SetupComboColumns(AreaGrid, 1, new[] { "Авто", "мм²", "см²", "м²" });
            SetupComboColumns(LengthGrid, 1, new[] { "Авто", "мм", "см", "м" });
            SetupComboColumns(MassGrid, 1, new[] { "Авто", "г", "кг", "т" });
            SetupComboColumns(VolumeCategoryGrid, 2, new[] { "Авто", "мм³", "л", "м³" });
            SetupComboColumns(AreaCategoryGrid, 2, new[] { "Авто", "мм²", "см²", "м²" });
            SetupComboColumns(LengthCategoryGrid, 2, new[] { "Авто", "мм", "см", "м" });
            SetupComboColumns(MassCategoryGrid, 2, new[] { "Авто", "г", "кг", "т" });

            InfoButton.ToolTip = new TextBlock
            {
                Text = InfoText,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 480
            };

            if (_document == null)
            {
                AnalyzeButton.IsEnabled = false;
                AnalyzeButton.ToolTip = "Нет открытого документа";
                foreach (Button pickButton in new[]
                {
                    VolumePickButton, AreaPickButton, LengthPickButton, MassPickButton,
                    VolumeCatPickButton, AreaCatPickButton, LengthCatPickButton, MassCatPickButton
                })
                {
                    pickButton.IsEnabled = false;
                    pickButton.ToolTip = "Нет открытого документа";
                }
            }

            LoadToUi(Settings);
        }

        private static void SetupComboColumns(DataGrid grid, int sourceColumnIndex, string[] unitLabels)
        {
            ((DataGridComboBoxColumn)grid.Columns[sourceColumnIndex]).ItemsSource = SourceLabels;
            ((DataGridComboBoxColumn)grid.Columns[sourceColumnIndex + 1]).ItemsSource = unitLabels;
        }

        /// <summary>Таблица, к которой относится нажатая кнопка секции (по Tag).</summary>
        private DataGrid ResolveGrid(object sender)
        {
            var button = sender as Button;
            string key = button != null ? button.Tag as string : null;
            return key != null && _gridsByKey.TryGetValue(key, out DataGrid grid) ? grid : null;
        }

        // ---------- загрузка/выгрузка ----------

        private void LoadToUi(PluginSettings settings)
        {
            FillRules(_volumeRules, settings.VolumeRules);
            FillRules(_areaRules, settings.AreaRules);
            FillRules(_lengthRules, settings.LengthRules);
            FillRules(_massRules, settings.MassRules);
            FillRules(_volumeCategoryRules, settings.VolumeCategoryRules);
            FillRules(_areaCategoryRules, settings.AreaCategoryRules);
            FillRules(_lengthCategoryRules, settings.LengthCategoryRules);
            FillRules(_massCategoryRules, settings.MassCategoryRules);

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
                    target.Add(new RuleVm(rule.Name, rule.Source, rule.Unit) { Category = rule.Category ?? string.Empty });
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            foreach (DataGrid grid in _gridRules.Keys)
            {
                grid.CommitEdit(DataGridEditingUnit.Cell, true);
                grid.CommitEdit(DataGridEditingUnit.Row, true);
            }

            // Исключение без категории не имеет смысла — не даём молча потерять строку.
            var incompleteTabs = new List<string>();
            if (HasRuleWithoutCategory(_volumeCategoryRules)) incompleteTabs.Add("Объем");
            if (HasRuleWithoutCategory(_areaCategoryRules)) incompleteTabs.Add("Площадь");
            if (HasRuleWithoutCategory(_lengthCategoryRules)) incompleteTabs.Add("Длина");
            if (HasRuleWithoutCategory(_massCategoryRules)) incompleteTabs.Add("Масса");
            if (incompleteTabs.Count > 0)
            {
                MessageBox.Show(this,
                    "В таблице исключений есть строки с параметром, но без категории (вкладки: "
                    + string.Join(", ", incompleteTabs)
                    + "). Укажите категорию или удалите такие строки.",
                    "Sminex BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var settings = new PluginSettings
            {
                Version = PluginSettings.CurrentVersion,
                VolumeRules = CollectGeneralRules(_volumeRules),
                AreaRules = CollectGeneralRules(_areaRules),
                LengthRules = CollectGeneralRules(_lengthRules),
                MassRules = CollectGeneralRules(_massRules),
                VolumeCategoryRules = CollectCategoryRules(_volumeCategoryRules),
                AreaCategoryRules = CollectCategoryRules(_areaCategoryRules),
                LengthCategoryRules = CollectCategoryRules(_lengthCategoryRules),
                MassCategoryRules = CollectCategoryRules(_massCategoryRules),
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

        private static bool HasRuleWithoutCategory(ObservableCollection<RuleVm> rules)
        {
            return rules.Any(vm => !string.IsNullOrWhiteSpace(vm.Name) && string.IsNullOrWhiteSpace(vm.Category));
        }

        private static List<ParameterRule> CollectGeneralRules(ObservableCollection<RuleVm> rules)
        {
            return rules
                .Where(vm => !string.IsNullOrWhiteSpace(vm.Name))
                .Select(vm => new ParameterRule(vm.Name.Trim(), vm.Source) { Unit = vm.Unit })
                .ToList();
        }

        private static List<ParameterRule> CollectCategoryRules(ObservableCollection<RuleVm> rules)
        {
            return rules
                .Where(vm => !string.IsNullOrWhiteSpace(vm.Name) && !string.IsNullOrWhiteSpace(vm.Category))
                .Select(vm => new ParameterRule(vm.Name.Trim(), vm.Source)
                {
                    Unit = vm.Unit,
                    Category = vm.Category.Trim()
                })
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

        private void Info_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(this, InfoText, "Sminex BIM Tools — как работают правила",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ---------- операции со строками таблиц (кнопки секций) ----------

        private void SectionAdd_Click(object sender, RoutedEventArgs e)
        {
            DataGrid grid = ResolveGrid(sender);
            if (grid == null)
                return;

            var vm = new RuleVm(string.Empty, ParameterSource.Auto, RawUnit.Auto);
            _gridRules[grid].Add(vm);

            grid.SelectedItem = vm;
            grid.ScrollIntoView(vm);

            // Сразу открываем редактирование первой колонки — понятно, что делать.
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                grid.Focus();
                grid.CurrentCell = new DataGridCellInfo(vm, grid.Columns[0]);
                grid.BeginEdit();
            }));
        }

        private void SectionRemove_Click(object sender, RoutedEventArgs e)
        {
            DataGrid grid = ResolveGrid(sender);
            if (grid != null && grid.SelectedItem is RuleVm vm)
                _gridRules[grid].Remove(vm);
        }

        private void SectionUp_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRule(ResolveGrid(sender), -1);
        }

        private void SectionDown_Click(object sender, RoutedEventArgs e)
        {
            MoveSelectedRule(ResolveGrid(sender), 1);
        }

        private void MoveSelectedRule(DataGrid grid, int delta)
        {
            if (grid == null || !(grid.SelectedItem is RuleVm vm))
                return;

            ObservableCollection<RuleVm> rules = _gridRules[grid];
            int index = rules.IndexOf(vm);
            int newIndex = index + delta;
            if (index < 0 || newIndex < 0 || newIndex >= rules.Count)
                return;

            rules.Move(index, newIndex);
            grid.SelectedItem = vm;
        }

        private void SectionPick_Click(object sender, RoutedEventArgs e)
        {
            DataGrid grid = ResolveGrid(sender);
            if (grid == null || _document == null)
                return;

            var picker = new ParameterPickerWindow(_document) { Owner = this };
            if (picker.ShowDialog() != true)
                return;

            ObservableCollection<RuleVm> rules = _gridRules[grid];
            var existing = new HashSet<string>(
                rules.Select(r => (r.Name ?? string.Empty).Trim()),
                StringComparer.OrdinalIgnoreCase);

            Dictionary<string, ProjectParameterInfo> map = ModelParameterAnalyzer.Collect(_document);
            MeasureKind kind = _gridKinds[grid];

            foreach (string name in picker.SelectedNames)
            {
                if (existing.Contains(name))
                    continue;
                rules.Add(new RuleVm(name, ParameterSource.Auto, RawUnit.Auto)
                {
                    Status = ModelParameterAnalyzer.Evaluate(name, kind, map)
                });
                existing.Add(name);
            }
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

            foreach (KeyValuePair<DataGrid, ObservableCollection<RuleVm>> pair in _gridRules)
            {
                MeasureKind kind = _gridKinds[pair.Key];
                foreach (RuleVm vm in pair.Value)
                    vm.Status = ModelParameterAnalyzer.Evaluate(vm.Name, kind, map);
            }
        }
    }

    /// <summary>Строка таблицы правил.</summary>
    public class RuleVm : INotifyPropertyChanged
    {
        private string _name;
        private string _category = string.Empty;
        private string _sourceLabel;
        private string _unitLabel;
        private string _status;

        public RuleVm(string name, ParameterSource source, RawUnit unit)
        {
            _name = name ?? string.Empty;
            _sourceLabel = ToLabel(source);
            _unitLabel = RawUnits.Label(unit);
            _status = string.Empty;
        }

        public string Category
        {
            get { return _category; }
            set { _category = value ?? string.Empty; OnPropertyChanged(nameof(Category)); }
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

        public string UnitLabel
        {
            get { return _unitLabel; }
            set { _unitLabel = value; OnPropertyChanged(nameof(UnitLabel)); }
        }

        public RawUnit Unit
        {
            get { return RawUnits.FromLabel(_unitLabel); }
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
