using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace XNode.Windows.ImageEditor
{
    public class NativeBlinkWindow
    {
        [DllImport("user32.dll")]
        private static extern IntPtr CreateWindowEx(
            uint dwExStyle, string lpClassName, string lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu,
            IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreatePen(int fnPenStyle, int nWidth, uint crColor);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool Rectangle(IntPtr hdc, int nLeftRect, int nTopRect, int nRightRect, int nBottomRect);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

        private const uint WS_EX_LAYERED = 0x80000;
        private const uint WS_EX_TRANSPARENT = 0x20;
        private const uint WS_EX_TOPMOST = 0x8;
        private const uint WS_EX_TOOLWINDOW = 0x80;
        private const uint WS_POPUP = 0x80000000;
        private const int SW_SHOW = 5;
        private const int SW_HIDE = 0;
        private const uint LWA_ALPHA = 0x2;
        private const uint LWA_COLORKEY = 0x1;

        private DispatcherTimer? _blinkTimer;
        private int _blinkCount;
        private const int MaxBlinkCount = 15; // 3秒 = 15次 * 200ms
        private Action? _onBlinkCompleted;
        private bool _isVisible = true;

        public void ShowBlinkAt(double x, double y, double width, double height, Action? onBlinkCompleted = null)
        {
            _onBlinkCompleted = onBlinkCompleted;

            try
            {
                Console.WriteLine($"=== 屏幕坐标直接画框 ===");
                Console.WriteLine($"左上角坐标: ({x:F0}, {y:F0})");
                Console.WriteLine($"图像尺寸: {width:F0}x{height:F0}");

                // 添加调试信息
                var screenWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
                var screenHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;
                Console.WriteLine($"屏幕尺寸: {screenWidth}x{screenHeight}");

                // 在屏幕范围内直接画框（直接传递左上角坐标）
                var blinkWindow = new BlinkOverlayWindow();
                blinkWindow.ShowBlinkAt(x, y, width, height, _onBlinkCompleted);

                Console.WriteLine($"========================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"画框错误: {ex.Message}");
                _onBlinkCompleted?.Invoke();
            }
        }
    }
}
