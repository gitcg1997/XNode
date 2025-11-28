using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XNode.Windows.ImageEditor.Helpers
{
    /// <summary>
    /// 基于ShareX的WindowsRectangleList实现
    /// 预枚举所有窗口和子控件,提供高效的鼠标位置查询
    /// </summary>
    public class WindowRectangleList
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("dwmapi.dll")]
        private static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out bool pvAttribute, int cbAttribute);

        private const int DWMWA_CLOAKED = 14;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public Rectangle ToRectangle() => new Rectangle(Left, Top, Width, Height);
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        #endregion

        public IntPtr IgnoreHandle { get; set; }
        public bool IncludeChildWindows { get; set; }
        public int Timeout { get; set; } = 5000;

        private List<WindowInfo> windows;
        private HashSet<IntPtr> parentHandles;
        private CancellationTokenSource cts;

        /// <summary>
        /// 获取所有可见窗口和子控件的信息列表
        /// </summary>
        public List<WindowInfo> GetWindowInfoList()
        {
            windows = new List<WindowInfo>();
            parentHandles = new HashSet<IntPtr>();

            try
            {
                if (Timeout > 0)
                {
                    cts = new CancellationTokenSource();
                    cts.CancelAfter(Timeout);
                }

                EnumWindows(CheckWindowHandle, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"枚举窗口错误: {ex.Message}");
            }
            finally
            {
                cts?.Dispose();
            }

            // 过滤被遮挡的子控件
            List<WindowInfo> result = new List<WindowInfo>();

            foreach (WindowInfo window in windows)
            {
                bool rectVisible = true;

                // 子控件检查是否被父窗口完全包含
                if (!window.IsWindow)
                {
                    foreach (WindowInfo window2 in result)
                    {
                        if (window2.Rectangle.Contains(window.Rectangle))
                        {
                            rectVisible = false;
                            break;
                        }
                    }
                }

                if (rectVisible)
                {
                    result.Add(window);
                }
            }

            return result;
        }

        /// <summary>
        /// 在预枚举的窗口列表中查找包含指定点的窗口
        /// 这比每次调用WindowFromPoint快得多
        /// </summary>
        public WindowInfo FindWindowAtPoint(List<WindowInfo> windowList, Point point)
        {
            if (windowList == null) return null;

            // 从后向前查找(后添加的窗口通常在上层)
            for (int i = windowList.Count - 1; i >= 0; i--)
            {
                if (windowList[i].Rectangle.Contains(point))
                {
                    return windowList[i];
                }
            }

            return null;
        }

        private bool CheckWindowHandle(IntPtr hWnd, IntPtr lParam)
        {
            return CheckHandle(hWnd, Rectangle.Empty, true);
        }

        private bool CheckHandle(IntPtr handle, Rectangle clipRect, bool isWindow)
        {
            // 检查是否超时
            if (cts != null && cts.IsCancellationRequested)
            {
                return false;
            }

            // 跳过不可见窗口和被隐藏的窗口
            if (handle == IgnoreHandle || !IsWindowVisible(handle))
            {
                return true;
            }

            // 检查窗口是否被DWM隐藏(Win10+)
            if (isWindow && IsWindowCloaked(handle))
            {
                return true;
            }

            WindowInfo windowInfo = new WindowInfo { Handle = handle };

            // 获取窗口矩形
            if (isWindow)
            {
                windowInfo.IsWindow = true;
                if (!GetWindowRect(handle, out RECT rect))
                {
                    return true;
                }
                windowInfo.Rectangle = rect.ToRectangle();
            }
            else
            {
                if (!GetWindowRect(handle, out RECT rect))
                {
                    return true;
                }
                Rectangle fullRect = rect.ToRectangle();
                windowInfo.Rectangle = Rectangle.Intersect(fullRect, clipRect);
            }

            // 验证矩形有效性
            if (windowInfo.Rectangle.Width <= 0 || windowInfo.Rectangle.Height <= 0)
            {
                return true;
            }

            // 获取窗口标题和类名
            var title = new StringBuilder(256);
            GetWindowText(handle, title, 256);
            windowInfo.Title = title.ToString();

            var className = new StringBuilder(256);
            GetClassName(handle, className, 256);
            windowInfo.ClassName = className.ToString();

            // 枚举子窗口
            if (IncludeChildWindows && !parentHandles.Contains(handle))
            {
                parentHandles.Add(handle);
                EnumChildWindows(handle, (hWnd, lParam) => CheckHandle(hWnd, windowInfo.Rectangle, false), IntPtr.Zero);
            }

            // 添加客户区矩形(如果与窗口矩形不同)
            if (isWindow && GetClientRect(handle, out RECT clientRect))
            {
                Rectangle clientRectangle = clientRect.ToRectangle();
                if (clientRectangle.Width > 0 && clientRectangle.Height > 0 &&
                    clientRectangle != windowInfo.Rectangle)
                {
                    windows.Add(new WindowInfo
                    {
                        Handle = handle,
                        Rectangle = clientRectangle,
                        Title = windowInfo.Title,
                        ClassName = windowInfo.ClassName,
                        IsWindow = false,
                        IsClientArea = true
                    });
                }
            }

            windows.Add(windowInfo);
            return true;
        }

        private bool IsWindowCloaked(IntPtr hWnd)
        {
            try
            {
                if (DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out bool cloaked, sizeof(bool)) == 0)
                {
                    return cloaked;
                }
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// 窗口信息
    /// </summary>
    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public Rectangle Rectangle { get; set; }
        public string Title { get; set; }
        public string ClassName { get; set; }
        public bool IsWindow { get; set; }
        public bool IsClientArea { get; set; }

        public override string ToString()
        {
            string type = IsClientArea ? "客户区" : (IsWindow ? "窗口" : "子控件");
            string title = string.IsNullOrEmpty(Title) ? ClassName : Title;
            return $"{type}: {title} ({Rectangle.Width}×{Rectangle.Height})";
        }
    }
}
