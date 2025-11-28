using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfUserControl = System.Windows.Controls.UserControl;
using WpfTextChangedEventArgs = System.Windows.Controls.TextChangedEventArgs;
using WpfDataFormats = System.Windows.DataFormats;
using WpfDataObject = System.Windows.DataObject;

namespace XNode.SubSystem.NodeLibSystem.Controls
{
    /// <summary>
    /// 为节点提供屏幕区域选择与手动编辑功能的控件
    /// </summary>
    public partial class RegionSelectorControl : WpfUserControl
    {
        private bool _suppressNotifications;

        public event EventHandler<RegionSelectedEventArgs>? RegionChanged;

        public RegionSelectorControl()
        {
            InitializeComponent();
            RegisterPasteHandler(XTextBox);
            RegisterPasteHandler(YTextBox);
            RegisterPasteHandler(WidthTextBox);
            RegisterPasteHandler(HeightTextBox);
            SetRegion(0, 0, 0, 0, notify: false);
        }

        public void SetRegion(int x, int y, int width, int height)
        {
            SetRegion(x, y, width, height, notify: true);
        }

        public void SetRegionSilently(int x, int y, int width, int height)
        {
            SetRegion(x, y, width, height, notify: false);
        }

        private void SetRegion(int x, int y, int width, int height, bool notify)
        {
            _suppressNotifications = true;

            XTextBox.Text = x.ToString();
            YTextBox.Text = y.ToString();
            WidthTextBox.Text = width.ToString();
            HeightTextBox.Text = height.ToString();

            _suppressNotifications = false;

            UpdateSummary(x, y, width, height);

            if (notify)
                RaiseRegionChanged(x, y, width, height);
        }

        public Rectangle GetRegion()
        {
            GetInputs(out int x, out int y, out int width, out int height);
            return new Rectangle(x, y, width, height);
        }

        private void SelectRegionButton_Click(object sender, RoutedEventArgs e)
        {
            var selector = new RegionSelectionWindow
            {
                Owner = Window.GetWindow(this)
            };

            selector.ShowDialog();

            if (selector.SelectionMade)
            {
                var rect = selector.SelectedRegion;
                SetRegion(rect.X, rect.Y, rect.Width, rect.Height);
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SetRegion(0, 0, 0, 0);
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitManualInput();
        }

        private void TextBox_TextChanged(object sender, WpfTextChangedEventArgs e)
        {
            if (_suppressNotifications)
                return;

            GetInputs(out int x, out int y, out int width, out int height);
            UpdateSummary(x, y, width, height);
        }

        private void NumericTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not WpfTextBox textBox)
                return;

            bool allowNegative = textBox == XTextBox || textBox == YTextBox;
            string prospective = textBox.Text.Insert(textBox.CaretIndex, e.Text);

            e.Handled = !IsValidNumericInput(prospective, allowNegative);
        }

        private void RegisterPasteHandler(WpfTextBox textBox)
        {
            WpfDataObject.AddPastingHandler(textBox, OnTextBoxPaste);
        }

        private void OnTextBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not WpfTextBox textBox)
                return;

            bool allowNegative = textBox == XTextBox || textBox == YTextBox;

            if (!e.DataObject.GetDataPresent(WpfDataFormats.Text))
            {
                e.CancelCommand();
                return;
            }

            var pastedText = e.DataObject.GetData(WpfDataFormats.Text) as string ?? string.Empty;

            string prospective = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
                .Insert(textBox.SelectionStart, pastedText);

            if (!IsValidNumericInput(prospective, allowNegative))
                e.CancelCommand();
        }

        private void CommitManualInput()
        {
            GetInputs(out int x, out int y, out int width, out int height);
            SetRegion(x, y, width, height);
        }

        private void GetInputs(out int x, out int y, out int width, out int height)
        {
            x = ParseOrDefault(XTextBox.Text, 0, allowNegative: true);
            y = ParseOrDefault(YTextBox.Text, 0, allowNegative: true);
            width = Math.Max(0, ParseOrDefault(WidthTextBox.Text, 0, allowNegative: false));
            height = Math.Max(0, ParseOrDefault(HeightTextBox.Text, 0, allowNegative: false));
        }

        private static int ParseOrDefault(string? text, int fallback, bool allowNegative)
        {
            if (string.IsNullOrWhiteSpace(text))
                return fallback;

            if (!allowNegative && text.Contains('-'))
                return fallback;

            return int.TryParse(text, out int value) ? value : fallback;
        }

        private static bool IsValidNumericInput(string text, bool allowNegative)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            if (allowNegative)
                return Regex.IsMatch(text, "^-?\\d*$");

            return Regex.IsMatch(text, "^\\d*$");
        }

        private void UpdateSummary(int x, int y, int width, int height)
        {
            SummaryText.Text = width > 0 && height > 0
                ? $"区域: X={x}, Y={y}, W={width}, H={height}"
                : "尚未选择有效区域";
        }

        private void RaiseRegionChanged(int x, int y, int width, int height)
        {
            if (_suppressNotifications)
                return;

            RegionChanged?.Invoke(this, new RegionSelectedEventArgs(new Rectangle(x, y, width, height)));
        }
    }

    public sealed class RegionSelectedEventArgs : EventArgs
    {
        public RegionSelectedEventArgs(Rectangle region)
        {
            Region = region;
        }

        public Rectangle Region { get; }
    }
}
