using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Automation;
using XNode.Windows.ImageEditor.Models;
using XNode.Windows.ImageEditor.Helpers;
using WpfPoint = System.Windows.Point;
using WpfRectangle = System.Windows.Shapes.Rectangle;

namespace XNode.Windows.ImageEditor
{
    public partial class CaptureWindow : Window
    {
        #region Win32 API
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pt);

        [DllImport("user32.dll")]
        private static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, POINT pt, uint uFlags);

        private const uint CWP_ALL = 0x0000;
        private const uint CWP_SKIPINVISIBLE = 0x0001;
        private const uint CWP_SKIPDISABLED = 0x0002;
        private const uint CWP_SKIPTRANSPARENT = 0x0004;

        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020;
        private const int WS_EX_LAYERED = 0x00080000;

        // 全局鼠标钩子
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;

            public System.Drawing.Rectangle ToRectangle() => new System.Drawing.Rectangle(Left, Top, Width, Height);
        }
        #endregion

        private WpfPoint _startPoint;
        private DispatcherTimer? _detectionTimer;
        private IntPtr _lastDetectedWindow = IntPtr.Zero;
        private AutomationElement? _lastDetectedElement = null;
        private CaptureMode _currentMode = CaptureMode.ModeSelection;
        private bool _useUIAutomation = true; // 默认使用 UI Automation
        private bool _isDetectionMode = false; // 是否处于检测模式（用于区分鼠标点击的用途）

        // ShareX风格的预枚举窗口列表
        private List<WindowInfo> _cachedWindows = null;
        private WindowInfo _currentHoverWindow = null;

        // 全局鼠标钩子
        private IntPtr _mouseHookHandle = IntPtr.Zero;
        private LowLevelMouseProc? _mouseProc;

        // 标记是否从 ImageEditor 调用（用于重新捕获场景）
        private bool _isRecaptureMode = false;

        public string? CapturedImagePath { get; private set; }
        public TaskItem? ResultTask { get; private set; }
        public bool CaptureSucceeded { get; private set; } = false;

        private enum CaptureMode
        {
            ModeSelection,      // 模式选择
            AutoDetect,         // 自动检测模式
            ManualDrag          // 手动拖拽模式
        }

        public CaptureWindow(bool isRecaptureMode = false)
        {
            InitializeComponent();
            _isRecaptureMode = isRecaptureMode;
            this.Loaded += CaptureWindow_Loaded;
            ShowModeSelection();
        }

        private void CaptureWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // 窗口加载后，默认禁用穿透（因为需要响应模式选择按钮的点击）
            SetWindowTransparent(false);
        }

        /// <summary>
        /// 安装全局鼠标钩子
        /// </summary>
        private void InstallMouseHook()
        {
            if (_mouseHookHandle != IntPtr.Zero)
                return; // 已经安装

            _mouseProc = MouseHookCallback;
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                if (curModule != null)
                {
                    _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                        GetModuleHandle(curModule.ModuleName), 0);
                    Console.WriteLine("全局鼠标钩子已安装");
                }
            }
        }

        /// <summary>
        /// 卸载全局鼠标钩子
        /// </summary>
        private void UninstallMouseHook()
        {
            if (_mouseHookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHookHandle);
                _mouseHookHandle = IntPtr.Zero;
                Console.WriteLine("全局鼠标钩子已卸载");
            }
        }

        /// <summary>
        /// 鼠标钩子回调
        /// </summary>
        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_LBUTTONDOWN)
            {
                if (_isDetectionMode)
                {
                    // 在UI线程上执行截图操作
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Console.WriteLine("检测到全局鼠标左键点击，触发自动截图");
                        AutoCaptureButton_Click(this, new RoutedEventArgs());
                    }));
                }
            }
            return CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
        }

        /// <summary>
        /// 设置窗口是否对鼠标和 UI Automation 透明
        /// </summary>
        /// <param name="transparent">true = 穿透（用于检测下方窗口），false = 不穿透（用于UI交互）</param>
        private void SetWindowTransparent(bool transparent)
        {
            try
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

                if (transparent)
                {
                    // 启用穿透：添加 WS_EX_TRANSPARENT 和 WS_EX_LAYERED
                    SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT | WS_EX_LAYERED);
                    Console.WriteLine("窗口穿透已启用");
                }
                else
                {
                    // 禁用穿透：移除 WS_EX_TRANSPARENT
                    SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle & ~WS_EX_TRANSPARENT);
                    Console.WriteLine("窗口穿透已禁用");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置窗口穿透失败: {ex.Message}");
            }
        }

        #region 模式选择
        private void ShowModeSelection()
        {
            _currentMode = CaptureMode.ModeSelection;
            ModeSelectionPanel.Visibility = Visibility.Visible;
            ManualDragPanel.Visibility = Visibility.Collapsed;
            AutoDetectPanel.Visibility = Visibility.Collapsed;
        }

        private void AutoDetectButton_Click(object sender, RoutedEventArgs e)
        {
            StartAutoDetectMode();
        }

        private void ManualDragButton_Click(object sender, RoutedEventArgs e)
        {
            StartManualDragMode();
        }
        #endregion

        #region 自动检测模式
        private async void StartAutoDetectMode()
        {
            _currentMode = CaptureMode.AutoDetect;
            _isDetectionMode = true;
            ModeSelectionPanel.Visibility = Visibility.Collapsed;
            AutoDetectPanel.Visibility = Visibility.Visible;
            ManualDragPanel.Visibility = Visibility.Collapsed;

            // 显示loading提示
            DetectionInfoText.Text = "⏳ 正在枚举窗口列表...\n这可能需要几秒钟";

            // 临时禁用窗口穿透，等待鼠标移动后再启用
            SetWindowTransparent(false);

            // 异步预枚举所有窗口(ShareX风格) - 后台线程执行
            await Task.Run(() =>
            {
                try
                {
                    var windowList = new WindowRectangleList
                    {
                        IgnoreHandle = Dispatcher.Invoke(() => new System.Windows.Interop.WindowInteropHelper(this).Handle),
                        IncludeChildWindows = true, // 包含子控件
                        Timeout = 5000
                    };
                    _cachedWindows = windowList.GetWindowInfoList();
                    Console.WriteLine($"✅ 预枚举完成,共 {_cachedWindows?.Count ?? 0} 个窗口/控件");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ 枚举窗口失败: {ex.Message}");
                    _cachedWindows = new List<WindowInfo>();
                }
            });

            // 更新UI提示
            DetectionInfoText.Text = $"✅ 已枚举 {_cachedWindows?.Count ?? 0} 个窗口/控件\n\n准备就绪,移动鼠标到目标窗口\n点击左键确认截图";

            // 安装全局鼠标钩子来捕获点击（因为窗口穿透后无法接收WPF事件）
            InstallMouseHook();

            // 延迟启用穿透，避免初始化时的问题
            var initTimer = new DispatcherTimer();
            initTimer.Interval = TimeSpan.FromMilliseconds(200);
            initTimer.Tick += (s, e) =>
            {
                initTimer.Stop();
                // 启用窗口穿透，让鼠标和 UI Automation 能检测到下方的窗口
                SetWindowTransparent(true);
                Console.WriteLine($"🚀 窗口穿透已启用,开始检测(共{_cachedWindows?.Count ?? 0}个窗口)");
            };
            initTimer.Start();

            // 绑定鼠标按下事件用于截图（作为备用，钩子失败时使用）
            this.MouseDown += OnDetectionMouseDown;

            // 启动检测定时器
            _detectionTimer = new DispatcherTimer();
            _detectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _detectionTimer.Tick += DetectionTimer_Tick;
            _detectionTimer.Start();

            this.Cursor = System.Windows.Input.Cursors.Cross;
        }

        private void OnDetectionMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 只在检测模式下响应鼠标左键
            if (_isDetectionMode && e.LeftButton == MouseButtonState.Pressed)
            {
                Console.WriteLine("检测到鼠标左键点击，触发自动截图");
                // 触发截图
                AutoCaptureButton_Click(this, new RoutedEventArgs());
            }
        }

        private void DetectionTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // 使用ShareX风格的预枚举列表检测
                if (_cachedWindows != null && _cachedWindows.Count > 0)
                {
                    DetectWithCachedList();
                }
                else if (_useUIAutomation)
                {
                    Console.WriteLine("⚠️ 使用UI Automation检测(预枚举列表为空)");
                    DetectWithUIAutomation();
                }
                else
                {
                    Console.WriteLine("⚠️ 使用Win32 API检测(预枚举列表为空)");
                    DetectWithWin32API();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检测错误: {ex.Message}");
            }
        }

        /// <summary>
        /// ShareX风格检测:使用WindowFromPoint+递归子控件检测
        /// </summary>
        private void DetectWithCachedList()
        {
            try
            {
                // 获取当前窗口句柄(用于排除自己)
                IntPtr thisWindowHandle = new System.Windows.Interop.WindowInteropHelper(this).Handle;

                // 获取鼠标位置
                GetCursorPos(out POINT cursorPos);

                // 使用WindowFromPoint获取顶层窗口
                IntPtr hwnd = WindowFromPoint(cursorPos);

                // 跳过自己的窗口
                if (hwnd == IntPtr.Zero || hwnd == thisWindowHandle)
                {
                    ClearDetectionHighlight();
                    DetectionInfoText.Text = "🔍 移动鼠标到窗口上自动检测\n🖱️ 点击左键截取检测到的窗口";
                    _currentHoverWindow = null;
                    _lastDetectedWindow = IntPtr.Zero;
                    return;
                }

                // 先尝试用Win32 API递归查找子控件
                IntPtr childHwnd = GetChildWindowAtPoint(hwnd, cursorPos);
                bool foundWin32Child = (childHwnd != IntPtr.Zero && childHwnd != hwnd);

                if (foundWin32Child)
                {
                    Console.WriteLine($"🔍 找到Win32子控件: 父窗口={hwnd:X}, 子控件={childHwnd:X}");
                    hwnd = childHwnd;
                }

                // 如果没找到Win32子控件,尝试UI Automation(适用于WPF/UWP等现代UI)
                if (!foundWin32Child)
                {
                    Console.WriteLine($"🔍 未找到Win32子控件,尝试UI Automation检测...");
                    var uiElement = TryGetUIAutomationElement(cursorPos);
                    if (uiElement != null)
                    {
                        // 使用UI Automation元素
                        DetectUIAutomationElement(uiElement, hwnd);
                        return; // 已显示UI元素信息,直接返回
                    }
                }

                // 在预枚举列表中查找这个窗口的详细信息
                WindowInfo? foundWindow = null;
                if (_cachedWindows != null && _cachedWindows.Count > 0)
                {
                    foundWindow = _cachedWindows.FirstOrDefault(w => w.Handle == hwnd);
                }

                // 如果在预枚举列表中找不到,使用Win32 API获取信息
                if (foundWindow == null)
                {
                    GetWindowRect(hwnd, out RECT rect);
                    var title = new StringBuilder(256);
                    GetWindowText(hwnd, title, 256);
                    var className = new StringBuilder(256);
                    GetClassName(hwnd, className, 256);

                    foundWindow = new WindowInfo
                    {
                        Handle = hwnd,
                        Rectangle = rect.ToRectangle(),
                        Title = title.ToString(),
                        ClassName = className.ToString(),
                        IsWindow = childHwnd == IntPtr.Zero || childHwnd == hwnd
                    };
                }

                // 检查是否找到新窗口
                if (foundWindow != null)
                {
                    // 如果是同一个窗口,不重复更新
                    if (_currentHoverWindow?.Handle == foundWindow.Handle)
                    {
                        return;
                    }

                    _currentHoverWindow = foundWindow;
                    _lastDetectedWindow = foundWindow.Handle;

                    // 显示窗口信息
                    string windowType = foundWindow.IsClientArea ? "客户区" :
                                       (foundWindow.IsWindow ? "窗口" : "子控件");
                    string title = string.IsNullOrEmpty(foundWindow.Title) ?
                                  foundWindow.ClassName : foundWindow.Title;

                    DetectionInfoText.Text = $"✅ 已检测到 {windowType}\n" +
                                           $"📝 {title}\n" +
                                           $"📐 {foundWindow.Rectangle.Width} × {foundWindow.Rectangle.Height}\n" +
                                           $"📍 ({foundWindow.Rectangle.X}, {foundWindow.Rectangle.Y})\n" +
                                           $"🖱️ 点击左键截取此窗口";

                    // 绘制高亮矩形
                    DrawDetectionHighlight(foundWindow.Rectangle);
                }
                else
                {
                    // 未找到窗口,清除高亮
                    _currentHoverWindow = null;
                    _lastDetectedWindow = IntPtr.Zero;
                    ClearDetectionHighlight();
                    DetectionInfoText.Text = "🔍 移动鼠标到窗口上自动检测\n🖱️ 点击左键截取检测到的窗口";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ShareX检测错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试使用UI Automation获取鼠标位置的元素
        /// </summary>
        private AutomationElement? TryGetUIAutomationElement(POINT cursorPos)
        {
            try
            {
                var screenPoint = new System.Windows.Point(cursorPos.X, cursorPos.Y);
                var element = AutomationElement.FromPoint(screenPoint);
                return element;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 显示UI Automation元素的详细信息
        /// </summary>
        private void DetectUIAutomationElement(AutomationElement element, IntPtr parentHwnd)
        {
            try
            {
                _lastDetectedElement = element;
                _lastDetectedWindow = parentHwnd;

                var boundingRect = element.Current.BoundingRectangle;
                var elementName = element.Current.Name;
                var elementType = element.Current.ControlType.ProgrammaticName.Replace("ControlType.", "");
                var automationId = element.Current.AutomationId;
                var className = element.Current.ClassName;

                // 尝试获取元素值
                string elementValue = "";
                try
                {
                    if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePattern))
                    {
                        elementValue = ((ValuePattern)valuePattern).Current.Value;
                    }
                }
                catch { }

                // 更新检测信息
                var info = $"✨ UI自动化元素\n" +
                          $"📝 {(string.IsNullOrEmpty(elementName) ? className : elementName)}\n" +
                          $"🎯 {elementType}\n" +
                          $"📐 {boundingRect.Width:F0} × {boundingRect.Height:F0}\n" +
                          $"📍 ({boundingRect.Left:F0}, {boundingRect.Top:F0})";

                if (!string.IsNullOrEmpty(elementValue))
                {
                    info += $"\n💬 {elementValue}";
                }

                info += "\n🖱️ 点击左键截取此元素";

                DetectionInfoText.Text = info;

                // 绘制高亮框
                var rect = new RECT
                {
                    Left = (int)boundingRect.Left,
                    Top = (int)boundingRect.Top,
                    Right = (int)boundingRect.Right,
                    Bottom = (int)boundingRect.Bottom
                };
                DrawDetectionHighlight(rect);

                Console.WriteLine($"✨ UI Automation检测到: {elementType} - {elementName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI Automation显示错误: {ex.Message}");
            }
        }

        /// <summary>
        /// 递归查找鼠标位置下的最深层子控件(ShareX风格)
        /// </summary>
        private IntPtr GetChildWindowAtPoint(IntPtr hwndParent, POINT screenPoint)
        {
            try
            {
                // 将屏幕坐标转换为父窗口客户区坐标
                POINT clientPoint = screenPoint;
                if (!ScreenToClient(hwndParent, ref clientPoint))
                {
                    return hwndParent;
                }

                // 查找子窗口(跳过不可见、禁用和透明的)
                IntPtr childHwnd = ChildWindowFromPointEx(
                    hwndParent,
                    clientPoint,
                    CWP_SKIPINVISIBLE | CWP_SKIPDISABLED | CWP_SKIPTRANSPARENT
                );

                // 如果没有找到子窗口,或找到的就是父窗口自己,返回父窗口
                if (childHwnd == IntPtr.Zero || childHwnd == hwndParent)
                {
                    return hwndParent;
                }

                // 递归查找更深层的子控件
                IntPtr deeperChild = GetChildWindowAtPoint(childHwnd, screenPoint);
                return deeperChild != IntPtr.Zero ? deeperChild : childHwnd;
            }
            catch
            {
                return hwndParent;
            }
        }

        private void DetectWithUIAutomation()
        {
            try
            {
                // 获取鼠标位置
                GetCursorPos(out POINT cursorPos);
                var screenPoint = new System.Windows.Point(cursorPos.X, cursorPos.Y);

                // 使用 UI Automation 获取鼠标下的元素
                var element = AutomationElement.FromPoint(screenPoint);

                if (element != null && element != _lastDetectedElement)
                {
                    _lastDetectedElement = element;

                    // 获取元素的边界矩形
                    var boundingRect = element.Current.BoundingRectangle;

                    if (!boundingRect.IsEmpty)
                    {
                        // 获取元素信息
                        var elementName = element.Current.Name;
                        var elementType = element.Current.ControlType.ProgrammaticName;
                        var automationId = element.Current.AutomationId;
                        var className = element.Current.ClassName;

                        // 尝试获取元素值
                        string elementValue = "";
                        try
                        {
                            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out object valuePattern))
                            {
                                elementValue = ((ValuePattern)valuePattern).Current.Value;
                            }
                        }
                        catch { }

                        // 更新检测信息显示
                        var info = $"✨ UI Automation 检测\n" +
                                  $"━━━━━━━━━━━━━━━━━━━━\n" +
                                  $"元素名称: {(string.IsNullOrEmpty(elementName) ? "<空>" : elementName)}\n" +
                                  $"控件类型: {elementType.Replace("ControlType.", "")}\n" +
                                  $"自动化ID: {(string.IsNullOrEmpty(automationId) ? "<空>" : automationId)}\n" +
                                  $"类名: {(string.IsNullOrEmpty(className) ? "<空>" : className)}\n";

                        if (!string.IsNullOrEmpty(elementValue))
                        {
                            info += $"元素值: {elementValue}\n";
                        }

                        info += $"━━━━━━━━━━━━━━━━━━━━\n" +
                               $"位置: ({boundingRect.Left:F0}, {boundingRect.Top:F0})\n" +
                               $"尺寸: {boundingRect.Width:F0} × {boundingRect.Height:F0}";

                        DetectionInfoText.Text = info;

                        // 绘制高亮框
                        var rect = new RECT
                        {
                            Left = (int)boundingRect.Left,
                            Top = (int)boundingRect.Top,
                            Right = (int)boundingRect.Right,
                            Bottom = (int)boundingRect.Bottom
                        };
                        DrawDetectionHighlight(rect);

                        // 保存最后检测到的窗口句柄（用于截图）
                        GetCursorPos(out POINT pos);
                        _lastDetectedWindow = WindowFromPoint(pos);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI Automation 检测错误: {ex.Message}");
                // 降级使用 Win32 API
                DetectWithWin32API();
            }
        }

        private void DetectWithWin32API()
        {
            GetCursorPos(out POINT cursorPos);
            IntPtr hwnd = WindowFromPoint(cursorPos);

            if (hwnd != IntPtr.Zero && hwnd != _lastDetectedWindow)
            {
                _lastDetectedWindow = hwnd;

                var windowTitle = new System.Text.StringBuilder(256);
                var className = new System.Text.StringBuilder(256);
                GetWindowText(hwnd, windowTitle, 256);
                GetClassName(hwnd, className, 256);

                GetWindowRect(hwnd, out RECT rect);

                // 更新检测信息显示
                DetectionInfoText.Text = $"🪟 Win32 API 检测\n" +
                                        $"━━━━━━━━━━━━━━━━━━━━\n" +
                                        $"窗口句柄: {hwnd}\n" +
                                        $"窗口标题: {windowTitle}\n" +
                                        $"窗口类名: {className}\n" +
                                        $"━━━━━━━━━━━━━━━━━━━━\n" +
                                        $"窗口位置: ({rect.Left}, {rect.Top})\n" +
                                        $"窗口尺寸: {rect.Right - rect.Left} × {rect.Bottom - rect.Top}";

                // 在检测到的窗口位置绘制高亮框
                DrawDetectionHighlight(rect);
            }
        }

        private void DrawDetectionHighlight(RECT rect)
        {
            try
            {
                // 将屏幕坐标转换为窗口坐标
                var topLeft = this.PointFromScreen(new WpfPoint(rect.Left, rect.Top));
                var bottomRight = this.PointFromScreen(new WpfPoint(rect.Right, rect.Bottom));

                var width = bottomRight.X - topLeft.X;
                var height = bottomRight.Y - topLeft.Y;

                // 确保在窗口范围内
                if (topLeft.X >= 0 && topLeft.Y >= 0 && topLeft.X < this.ActualWidth && topLeft.Y < this.ActualHeight)
                {
                    Canvas.SetLeft(DetectionHighlight, topLeft.X);
                    Canvas.SetTop(DetectionHighlight, topLeft.Y);
                    DetectionHighlight.Width = Math.Max(0, width);
                    DetectionHighlight.Height = Math.Max(0, height);
                    DetectionHighlight.Visibility = Visibility.Visible;
                }
                else
                {
                    DetectionHighlight.Visibility = Visibility.Collapsed;
                }
            }
            catch
            {
                DetectionHighlight.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// System.Drawing.Rectangle重载版本
        /// </summary>
        private void DrawDetectionHighlight(System.Drawing.Rectangle rect)
        {
            RECT winRect = new RECT
            {
                Left = rect.Left,
                Top = rect.Top,
                Right = rect.Right,
                Bottom = rect.Bottom
            };
            DrawDetectionHighlight(winRect);
        }

        /// <summary>
        /// 清除检测高亮
        /// </summary>
        private void ClearDetectionHighlight()
        {
            try
            {
                DetectionHighlight.Visibility = Visibility.Collapsed;
            }
            catch { }
        }

        private void AutoCaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止检测定时器
            _detectionTimer?.Stop();

            // 禁用检测模式，防止再次触发
            _isDetectionMode = false;

            // 卸载全局鼠标钩子
            UninstallMouseHook();

            RECT rect;

            // 优先使用 UI Automation 元素的边界
            if (_useUIAutomation && _lastDetectedElement != null)
            {
                try
                {
                    var boundingRect = _lastDetectedElement.Current.BoundingRectangle;
                    if (!boundingRect.IsEmpty)
                    {
                        rect = new RECT
                        {
                            Left = (int)boundingRect.Left,
                            Top = (int)boundingRect.Top,
                            Right = (int)boundingRect.Right,
                            Bottom = (int)boundingRect.Bottom
                        };
                        CaptureScreenArea(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"使用 UI Automation 元素截图失败: {ex.Message}");
                }
            }

            // 降级使用窗口句柄
            if (_lastDetectedWindow != IntPtr.Zero)
            {
                GetWindowRect(_lastDetectedWindow, out rect);
                CaptureScreenArea(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            else
            {
                System.Windows.MessageBox.Show("未检测到有效的窗口或元素！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        #endregion

        #region 手动拖拽模式
        private void StartManualDragMode()
        {
            _currentMode = CaptureMode.ManualDrag;
            ModeSelectionPanel.Visibility = Visibility.Collapsed;
            AutoDetectPanel.Visibility = Visibility.Collapsed;
            ManualDragPanel.Visibility = Visibility.Visible;

            // 绑定鼠标事件
            this.MouseDown += OnMouseDown;
            this.MouseMove += OnMouseMove;
            this.MouseUp += OnMouseUp;

            this.Cursor = System.Windows.Input.Cursors.Cross;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != CaptureMode.ManualDrag)
                return;

            _startPoint = e.GetPosition(this);
            Canvas.SetLeft(SelectionRectangle, _startPoint.X);
            Canvas.SetTop(SelectionRectangle, _startPoint.Y);
            SelectionRectangle.Width = 0;
            SelectionRectangle.Height = 0;
            SelectionRectangle.Visibility = Visibility.Visible;
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_currentMode != CaptureMode.ManualDrag || e.LeftButton == MouseButtonState.Released)
                return;

            var pos = e.GetPosition(this);
            var x = Math.Min(pos.X, _startPoint.X);
            var y = Math.Min(pos.Y, _startPoint.Y);
            var w = Math.Abs(pos.X - _startPoint.X);
            var h = Math.Abs(pos.Y - _startPoint.Y);

            Canvas.SetLeft(SelectionRectangle, x);
            Canvas.SetTop(SelectionRectangle, y);
            SelectionRectangle.Width = w;
            SelectionRectangle.Height = h;

            // 更新信息面板
            UpdateInfoPanel(x, y, w, h);
        }

        private void UpdateInfoPanel(double x, double y, double width, double height)
        {
            if (width > 10 && height > 10) // 只有在选择区域足够大时才显示信息面板
            {
                InfoPanel.Visibility = Visibility.Visible;
                CoordinateText.Text = $"位置: ({(int)x}, {(int)y})";
                SizeText.Text = $"大小: {(int)width} × {(int)height}";

                // 定位信息面板到选择区域的右下角
                var panelX = x + width + 10;
                var panelY = y + height + 10;

                // 确保信息面板不会超出屏幕边界
                if (panelX + InfoPanel.ActualWidth > this.ActualWidth)
                    panelX = x - InfoPanel.ActualWidth - 10;
                if (panelY + InfoPanel.ActualHeight > this.ActualHeight)
                    panelY = y - InfoPanel.ActualHeight - 10;

                Canvas.SetLeft(InfoPanel, Math.Max(0, panelX));
                Canvas.SetTop(InfoPanel, Math.Max(0, panelY));
            }
            else
            {
                InfoPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentMode != CaptureMode.ManualDrag)
                return;

            var pos = e.GetPosition(this);
            var w = (int)Math.Abs(pos.X - _startPoint.X);
            var h = (int)Math.Abs(pos.Y - _startPoint.Y);

            if (w > 10 && h > 10) // 确保选择区域足够大
            {
                // 将窗口坐标转换为屏幕坐标
                var screenStartPoint = this.PointToScreen(new WpfPoint(_startPoint.X, _startPoint.Y));
                var screenEndPoint = this.PointToScreen(new WpfPoint(pos.X, pos.Y));

                var screenX = (int)Math.Min(screenStartPoint.X, screenEndPoint.X);
                var screenY = (int)Math.Min(screenStartPoint.Y, screenEndPoint.Y);
                var screenW = (int)Math.Abs(screenEndPoint.X - screenStartPoint.X);
                var screenH = (int)Math.Abs(screenEndPoint.Y - screenStartPoint.Y);

                CaptureScreenArea(screenX, screenY, screenW, screenH);
            }
            else
            {
                this.Close();
            }
        }
        #endregion

        #region 通用截图方法
        private void CaptureScreenArea(int x, int y, int width, int height)
        {
            this.Hide(); // 隐藏窗口后再截图

            try
            {
                System.Threading.Thread.Sleep(100); // 等待窗口完全隐藏

                var rect = new System.Drawing.Rectangle(x, y, width, height);

                using (var bmp = new Bitmap(rect.Width, rect.Height, PixelFormat.Format32bppArgb))
                {
                    using (var g = Graphics.FromImage(bmp))
                    {
                        g.CopyFromScreen(rect.Left, rect.Top, 0, 0, bmp.Size);
                    }

                    string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
                    Directory.CreateDirectory(dir);
                    string fileName = $"capture_{DateTime.Now:yyyyMMddHHmmssfff}.png";
                    CapturedImagePath = System.IO.Path.Combine(dir, fileName);
                    bmp.Save(CapturedImagePath, ImageFormat.Png);
                }

                // 标记截图成功
                CaptureSucceeded = true;

                // 如果是重新捕获模式，只返回图片路径，不打开新的编辑器窗口
                if (_isRecaptureMode)
                {
                    // 设置 DialogResult，使用 CaptureSucceeded 标志
                    SafeSetDialogResult(true);
                }
                else
                {
                    // 打开图像编辑器窗口
                    var imageEditor = new ImageEditorWindow(CapturedImagePath);
                    var result = imageEditor.ShowDialog();

                    if (result == true && imageEditor.IsCompleted)
                    {
                        ResultTask = imageEditor.ResultTask;
                        SafeSetDialogResult(true);
                    }
                    else
                    {
                        // 用户取消或关闭编辑器，仍然标记为成功（截图已保存）
                        SafeSetDialogResult(true);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"截图失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                CaptureSucceeded = false;
                SafeSetDialogResult(false);
            }

            this.Close();
        }

        /// <summary>
        /// 尝试将截图添加到图片库
        /// </summary>
        private void TryAddToImageLibrary(string imagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(imagePath) || !System.IO.File.Exists(imagePath))
                {
                    return;
                }

                // XNode 项目中暂时不需要保存到图像库
                // var imageLibraryService = new XNode.Windows.ImageEditor.Services.ImageLibraryService();
                var fileName = System.IO.Path.GetFileNameWithoutExtension(imagePath);

                // 生成更友好的名称
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                // var libraryItem = imageLibraryService.AddImage(
                //     imagePath,
                //     fileName,
                //     $"截图于 {timestamp}",
                //     "截图"
                // );

                Console.WriteLine($"图像已保存: {fileName} (时间: {timestamp})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加到图片库失败: {ex.Message}");
                // 不阻塞截图流程，只记录错误
            }
        }

        /// <summary>
        /// 安全地设置DialogResult,避免在非对话框模式下抛出异常
        /// </summary>
        private void SafeSetDialogResult(bool? value)
        {
            try
            {
                // 只有在窗口是对话框模式时才设置DialogResult
                // 通过检查ComponentDispatcher.IsThreadModal来判断是否是对话框模式
                if (System.Windows.Interop.ComponentDispatcher.IsThreadModal)
                {
                    this.DialogResult = value;
                }
            }
            catch (InvalidOperationException)
            {
                // 如果设置失败，忽略异常
                // 窗口可能不是通过ShowDialog()打开的
            }
        }
        #endregion

        #region 窗口事件处理
        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.Space || e.Key == Key.Enter)
            {
                // 空格键或回车键触发自动截图
                if (_currentMode == CaptureMode.AutoDetect)
                {
                    AutoCaptureButton_Click(this, new RoutedEventArgs());
                }
            }
            else if (e.Key == Key.Back || e.Key == Key.B)
            {
                // 退格键或B键返回模式选择
                if (_currentMode != CaptureMode.ModeSelection)
                {
                    BackToModeSelection_Click(this, new RoutedEventArgs());
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void BackToModeSelection_Click(object sender, RoutedEventArgs e)
        {
            // 停止检测定时器
            _detectionTimer?.Stop();

            // 解绑鼠标事件
            this.MouseDown -= OnMouseDown;
            this.MouseMove -= OnMouseMove;
            this.MouseUp -= OnMouseUp;
            this.MouseDown -= OnDetectionMouseDown;

            // 重置检测模式标志
            _isDetectionMode = false;

            // 卸载全局鼠标钩子
            UninstallMouseHook();

            // 禁用窗口穿透，恢复正常交互
            SetWindowTransparent(false);

            // 返回模式选择
            ShowModeSelection();
        }

        protected override void OnClosed(EventArgs e)
        {
            _detectionTimer?.Stop();
            this.MouseDown -= OnMouseDown;
            this.MouseMove -= OnMouseMove;
            this.MouseUp -= OnMouseUp;
            this.MouseDown -= OnDetectionMouseDown;
            _isDetectionMode = false;

            // 卸载全局鼠标钩子
            UninstallMouseHook();

            base.OnClosed(e);
        }
        #endregion
    }
}
