using System;
using System.Windows;
using System.Windows.Threading;
using Autodesk.Revit.UI;
using SminexBimTools.Core;

namespace SminexBimTools.UI
{
    /// <summary>
    /// Окно результата суммирования. В отличие от TaskDialog весь текст
    /// здесь выделяется мышью, а кнопка «Копировать» кладёт значение
    /// в буфер обмена.
    /// </summary>
    public partial class ResultWindow : Window
    {
        private readonly string _copyText;
        private readonly UIDocument _uidoc;
        private readonly SummationResult _selectionResult;
        private DispatcherTimer _copyFeedbackTimer;

        /// <param name="header">Заголовок с итогом (крупным шрифтом).</param>
        /// <param name="copyText">Что копирует кнопка «Копировать».</param>
        /// <param name="content">Основной текст.</param>
        /// <param name="expandedContent">Текст раздела «Подробности» (null — раздел скрыт).</param>
        /// <param name="uidoc">Документ для выбора элементов (null — кнопка выбора скрыта).</param>
        /// <param name="selectionResult">Результат для окна выбора элементов.</param>
        public ResultWindow(string header, string copyText, string content, string expandedContent,
            UIDocument uidoc = null, SummationResult selectionResult = null)
        {
            InitializeComponent();
            _copyText = copyText;
            _uidoc = uidoc;
            _selectionResult = selectionResult;

            HeaderText.Text = header;
            ContentText.Text = content ?? string.Empty;
            ContentText.Visibility = string.IsNullOrEmpty(content) ? Visibility.Collapsed : Visibility.Visible;

            if (!string.IsNullOrEmpty(expandedContent))
            {
                ExpandedText.Text = expandedContent;
                DetailsExpander.Visibility = Visibility.Visible;
            }

            if (_uidoc != null && _selectionResult != null)
                SelectButton.Visibility = Visibility.Visible;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            if (!TrySetClipboard(_copyText))
            {
                MessageBox.Show(this,
                    "Не удалось получить доступ к буферу обмена. Попробуйте еще раз.",
                    "Sminex BIM Tools",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ShowCopyFeedback();
        }

        /// <summary>
        /// Буфер обмена может быть занят другим приложением —
        /// делаем несколько попыток, прежде чем сдаться.
        /// </summary>
        private static bool TrySetClipboard(string text)
        {
            for (int attempt = 0; attempt < 3; attempt++)
            {
                try
                {
                    Clipboard.SetDataObject(text ?? string.Empty, true);
                    return true;
                }
                catch (Exception)
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            return false;
        }

        private void ShowCopyFeedback()
        {
            CopyButton.Content = "Скопировано ✓";
            if (_copyFeedbackTimer == null)
            {
                _copyFeedbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
                _copyFeedbackTimer.Tick += (s, args) =>
                {
                    _copyFeedbackTimer.Stop();
                    CopyButton.Content = "Копировать";
                };
            }
            _copyFeedbackTimer.Stop();
            _copyFeedbackTimer.Start();
        }

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            var window = new SelectElementsWindow(_uidoc, _selectionResult)
            {
                Owner = this
            };
            window.ShowDialog();
        }
    }
}
