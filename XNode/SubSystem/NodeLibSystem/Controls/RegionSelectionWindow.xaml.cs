using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using DrawingRectangle = System.Drawing.Rectangle;
using WpfPoint = System.Windows.Point;

namespace XNode.SubSystem.NodeLibSystem.Controls
{
    /// <summary>
    /// 全屏区域选择窗口,允许用户拖拽框选屏幕区域
    /// </summary>
    public partial class RegionSelectionWindow : Window
    {
        private WpfPoint _startPoint;
        private bool _isSelecting;

        public bool SelectionMade { get; private set; }

        public DrawingRectangle SelectedRegion { get; private set; }

        public RegionSelectionWindow()
        {
            InitializeComponent();
            Loaded += (_, _) => SelectionCanvas.Focus();
        }

        private void SelectionCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(SelectionCanvas);

            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;

            InfoPanel.Visibility = Visibility.Collapsed;

            SelectionCanvas.CaptureMouse();
        }

        private void SelectionCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting || e.LeftButton != MouseButtonState.Pressed)
                return;

            var current = e.GetPosition(SelectionCanvas);
            var x = Math.Min(current.X, _startPoint.X);
            var y = Math.Min(current.Y, _startPoint.Y);
            var width = Math.Abs(current.X - _startPoint.X);
            var height = Math.Abs(current.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;

            UpdateInfoPanel(x, y, width, height);
        }

        private void SelectionCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isSelecting)
                return;

            _isSelecting = false;
            SelectionCanvas.ReleaseMouseCapture();

            var endPoint = e.GetPosition(SelectionCanvas);
            var width = Math.Abs(endPoint.X - _startPoint.X);
            var height = Math.Abs(endPoint.Y - _startPoint.Y);

            if (width < 5 || height < 5)
            {
                CancelSelection();
                return;
            }

            var region = CalculateDeviceRectangle(_startPoint, endPoint);
            if (region.Width <= 0 || region.Height <= 0)
            {
                CancelSelection();
                return;
            }

            SelectedRegion = region;
            SelectionMade = true;

            CloseWithResult(true);
        }

        private void UpdateInfoPanel(double x, double y, double width, double height)
        {
            if (width < 5 || height < 5)
            {
                InfoPanel.Visibility = Visibility.Collapsed;
                return;
            }

            CoordinateText.Text = $"位置: {(int)x}, {(int)y}";
            SizeText.Text = $"大小: {(int)width} x {(int)height}";

            InfoPanel.Visibility = Visibility.Visible;

            var panelX = x + width + 12;
            var panelY = y + height + 12;

            if (panelX + InfoPanel.ActualWidth > SelectionCanvas.ActualWidth)
                panelX = x - InfoPanel.ActualWidth - 12;

            if (panelY + InfoPanel.ActualHeight > SelectionCanvas.ActualHeight)
                panelY = y - InfoPanel.ActualHeight - 12;

            Canvas.SetLeft(InfoPanel, Math.Max(0, panelX));
            Canvas.SetTop(InfoPanel, Math.Max(0, panelY));
        }

        private DrawingRectangle CalculateDeviceRectangle(WpfPoint start, WpfPoint end)
        {
            var topLeft = new WpfPoint(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y));
            var bottomRight = new WpfPoint(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y));

            var screenTopLeft = SelectionCanvas.PointToScreen(topLeft);
            var screenBottomRight = SelectionCanvas.PointToScreen(bottomRight);

            var source = PresentationSource.FromVisual(this);
            var transform = source?.CompositionTarget?.TransformToDevice ?? Matrix.Identity;

            var deviceTopLeft = transform.Transform(screenTopLeft);
            var deviceBottomRight = transform.Transform(screenBottomRight);

            int x = (int)Math.Round(deviceTopLeft.X);
            int y = (int)Math.Round(deviceTopLeft.Y);
            int w = (int)Math.Round(deviceBottomRight.X - deviceTopLeft.X);
            int h = (int)Math.Round(deviceBottomRight.Y - deviceTopLeft.Y);

            return new DrawingRectangle(x, y, w, h);
        }

        private void CancelSelection()
        {
            SelectionMade = false;
            SelectedRegion = DrawingRectangle.Empty;
            CloseWithResult(false);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
                CancelSelection();
        }

        private void CloseWithResult(bool? result)
        {
            TrySetDialogResult(result);
            Close();
        }

        private void TrySetDialogResult(bool? value)
        {
            if (!ComponentDispatcher.IsThreadModal)
                return;

            try
            {
                DialogResult = value;
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
