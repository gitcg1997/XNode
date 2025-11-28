using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace XNode.Windows.ImageEditor
{
    public partial class BlinkOverlayWindow : Window
    {
        private DispatcherTimer _blinkTimer;
        private int _blinkCount;
        private const int MaxBlinkCount = 15; // 3秒 = 15次 * 200ms
        private Action? _onBlinkCompleted;

        public BlinkOverlayWindow()
        {
            InitializeComponent();
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Interval = TimeSpan.FromMilliseconds(200);
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        public void ShowBlinkAt(double x, double y, double width, double height, Action? onBlinkCompleted = null)
        {
            _onBlinkCompleted = onBlinkCompleted;

            try
            {
                Console.WriteLine($"=== 直接在屏幕坐标位置画框 ===");
                Console.WriteLine($"窗口位置: ({x:F0}, {y:F0})");
                Console.WriteLine($"窗口尺寸: {width:F0}x{height:F0}");

                // 窗口样式设置
                this.WindowState = WindowState.Normal;
                this.WindowStyle = WindowStyle.None;
                this.AllowsTransparency = true;
                this.Background = System.Windows.Media.Brushes.Transparent;
                this.Topmost = true;
                this.ResizeMode = ResizeMode.NoResize;

                // 直接设置窗口位置和尺寸为目标坐标
                this.Left = x;
                this.Top = y;
                this.Width = width;
                this.Height = height;

                // 方框填充整个窗口（Margin=0，宽高撑满）
                BlinkRectangle.Margin = new Thickness(0);
                BlinkRectangle.Width = width;
                BlinkRectangle.Height = height;
                BlinkRectangle.Stroke = System.Windows.Media.Brushes.Red;
                BlinkRectangle.StrokeThickness = 3;

                Console.WriteLine($"========================");

                // 显示窗口和开始闪烁
                this.Show();
                BlinkRectangle.Visibility = Visibility.Visible;
                _blinkCount = 0;
                _blinkTimer.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"方框绘制错误: {ex.Message}");
                _onBlinkCompleted?.Invoke();
            }
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            _blinkCount++;
            
            // 切换可见性实现闪烁效果
            BlinkRectangle.Visibility = BlinkRectangle.Visibility == Visibility.Visible 
                ? Visibility.Collapsed 
                : Visibility.Visible;
            
            // 达到最大闪烁次数后停止
            if (_blinkCount >= MaxBlinkCount)
            {
                _blinkTimer.Stop();
                this.Hide();
                _blinkCount = 0;

                // 调用完成回调
                _onBlinkCompleted?.Invoke();
                _onBlinkCompleted = null;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _blinkTimer?.Stop();
            base.OnClosed(e);
        }
    }
}
