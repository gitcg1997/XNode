using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Automation;
using XNode.Windows.ImageEditor.Services;
using XNode.Windows.ImageEditor.Services.Interfaces;
using XNode.Windows.ImageEditor.ImageRecognition;
using XNode.Windows.ImageEditor.Models;
using Microsoft.Extensions.Logging;

namespace XNode.Windows.ImageEditor
{
    public partial class ImageEditorWindow : Window
    {
        private string _imagePath;
        private readonly IImageRecognitionService _imageRecognitionService;
        private readonly IImageRecognitionEngine _imageRecognitionEngine;
        private DispatcherTimer _blinkTimer;
        private int _blinkCount;
        private DispatcherTimer _mouseTrackingTimer;
        private NativeBlinkWindow? _currentBlinkWindow;
        private DispatcherTimer? _detectionTimer;
        private bool _isDetecting = false;
        private string _largeSavePath = string.Empty; // 大文件保存路径

        public TaskItem? ResultTask { get; private set; }
        public bool IsCompleted { get; private set; }

        // Win32 API for getting cursor position
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);

        // Win32 API for setting cursor position
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int x, int y);

        // Win32 API for getting window from point
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(Point pt);

        // Win32 API for getting child window from point (more comprehensive)
        [DllImport("user32.dll")]
        private static extern IntPtr ChildWindowFromPointEx(IntPtr hwndParent, Point pt, uint flags);

        // Win32 API for converting screen to client coordinates
        [DllImport("user32.dll")]
        private static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

        // Flags for ChildWindowFromPointEx
        private const uint CWP_ALL = 0x0000;
        private const uint CWP_SKIPINVISIBLE = 0x0001;
        private const uint CWP_SKIPDISABLED = 0x0002;
        private const uint CWP_SKIPTRANSPARENT = 0x0004;

        // Win32 API for getting window text
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

        // Win32 API for getting class name
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

        // Win32 API for getting window rect
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        // Win32 API for capturing window content
        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const int SRCCOPY = 0x00CC0020;

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
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
        }

        public ImageEditorWindow(string imagePath = null)
        {
            InitializeComponent();
            _imagePath = imagePath ?? string.Empty;

            // Manually create dependencies for ImageRecognitionService
            // TODO: Consider refactoring to use DI in the future
            var loggerFactory = LoggerFactory.Create(builder => builder.AddDebug().AddConsole());
            _imageRecognitionEngine = new ImageRecognitionEngine();
            var screenshotService = new ScreenshotService(loggerFactory.CreateLogger<ScreenshotService>());
            _imageRecognitionService = new ImageRecognitionService(
                _imageRecognitionEngine,
                screenshotService,
                loggerFactory.CreateLogger<ImageRecognitionService>());

            LoadImage();
            InitializeBlinkTimer();
            InitializeValidationStatus();

            // 延迟启动鼠标跟踪，确保窗口完全加载
            this.Loaded += ImageEditorWindow_Loaded;
            
            // 处理窗口关闭事件，确保DialogResult被设置
            this.Closing += ImageEditorWindow_Closing;
        }

        private void ImageEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeMouseTracking();

            // 确保坐标文本框有正确的默认值
            InitializeCoordinateTextBoxes();

            // 从配置加载大文件保存路径
            LoadSavePath();
        }

        private void InitializeCoordinateTextBoxes()
        {
            try
            {
                // 初始化X坐标文本框
                if (MouseXTextBox != null)
                {
                    MouseXTextBox.IsEnabled = true;
                    MouseXTextBox.IsReadOnly = false;
                    MouseXTextBox.Visibility = System.Windows.Visibility.Visible;

                    // 设置样式确保可见性
                    MouseXTextBox.Background = System.Windows.Media.Brushes.White;
                    MouseXTextBox.Foreground = System.Windows.Media.Brushes.Black;
                    MouseXTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
                    MouseXTextBox.BorderThickness = new System.Windows.Thickness(1);

                    MouseXTextBox.Text = "0";
                }

                // 初始化Y坐标文本框
                if (MouseYTextBox != null)
                {
                    MouseYTextBox.IsEnabled = true;
                    MouseYTextBox.IsReadOnly = false;
                    MouseYTextBox.Visibility = System.Windows.Visibility.Visible;

                    // 设置样式确保可见性
                    MouseYTextBox.Background = System.Windows.Media.Brushes.White;
                    MouseYTextBox.Foreground = System.Windows.Media.Brushes.Black;
                    MouseYTextBox.BorderBrush = System.Windows.Media.Brushes.Gray;
                    MouseYTextBox.BorderThickness = new System.Windows.Thickness(1);

                    MouseYTextBox.Text = "0";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化坐标文本框错误: {ex.Message}");
            }
        }

        private void InitializeValidationStatus()
        {
            // 初始化时隐藏状态面板
            ValidationStatusPanel.Visibility = Visibility.Collapsed;
        }

        private void LoadImage()
        {
            try
            {
                if (File.Exists(_imagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.UriSource = new Uri(_imagePath);
                    bitmap.EndInit();

                    PreviewImage.Source = bitmap;

                    // 更新图像信息
                    UpdateImageInfo(bitmap);

                    // 设置默认元素名称
                    ElementNameTextBox.Text = $"图像元素_{DateTime.Now:HHmmss}";

                    // 启用相关按钮
                    EnableButtonsAfterImageLoaded();
                }
                else
                {
                    // 如果没有图片，禁用按钮
                    DisableButtonsWhenNoImage();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载图像失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                DisableButtonsWhenNoImage();
            }
        }

        /// <summary>
        /// 图片加载后启用相关按钮
        /// </summary>
        private void EnableButtonsAfterImageLoaded()
        {
            RecaptureButton.IsEnabled = true;
            ValidateButton.IsEnabled = true;
            SaveImageButton.IsEnabled = true;
        }

        /// <summary>
        /// 没有图片时禁用相关按钮
        /// </summary>
        private void DisableButtonsWhenNoImage()
        {
            RecaptureButton.IsEnabled = false;
            ValidateButton.IsEnabled = false;
            SaveImageButton.IsEnabled = false;
        }

        private void UpdateImageInfo(BitmapImage bitmap)
        {
            try
            {
                // 更新图像尺寸信息
                ImageWidthText.Text = bitmap.PixelWidth.ToString();
                ImageHeightText.Text = bitmap.PixelHeight.ToString();

                // 计算文件大小
                var fileInfo = new FileInfo(_imagePath);
                var sizeInKB = fileInfo.Length / 1024.0;
                ImageSizeText.Text = sizeInKB < 1024
                    ? $"{sizeInKB:F1} KB"
                    : $"{sizeInKB / 1024:F1} MB";
            }
            catch (Exception ex)
            {
                // 如果获取信息失败，显示默认值
                ImageWidthText.Text = "N/A";
                ImageHeightText.Text = "N/A";
                ImageSizeText.Text = "N/A";
            }
        }

        private void InitializeBlinkTimer()
        {
            _blinkTimer = new DispatcherTimer();
            _blinkTimer.Interval = TimeSpan.FromMilliseconds(200);
            _blinkTimer.Tick += BlinkTimer_Tick;
        }

        private void InitializeMouseTracking()
        {
            try
            {
                _mouseTrackingTimer = new DispatcherTimer();
                _mouseTrackingTimer.Interval = TimeSpan.FromMilliseconds(50); // 更新频率：20fps
                _mouseTrackingTimer.Tick += MouseTrackingTimer_Tick;
                _mouseTrackingTimer.Start();

                // 立即执行一次更新
                MouseTrackingTimer_Tick(this, EventArgs.Empty);

                Console.WriteLine("鼠标位置跟踪已启动");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"初始化鼠标跟踪失败: {ex.Message}");
                if (MousePositionLabel != null)
                {
                    MousePositionLabel.Text = "鼠标跟踪初始化失败";
                }
            }
        }

        private void MouseTrackingTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // 检查控件是否存在
                if (MousePositionLabel == null)
                {
                    Console.WriteLine("MousePositionLabel 控件为空");
                    return;
                }

                // 获取当前鼠标位置
                if (GetCursorPos(out Point cursorPos))
                {
                    // 获取DPI缩放比例
                    var dpiScale = GetSystemDpiScale();

                    // 显示物理像素坐标（这是图像识别使用的坐标）
                    MousePositionLabel.Text = $"鼠标位置: ({cursorPos.X}, {cursorPos.Y}) | DPI: {dpiScale:F2}x";
                }
                else
                {
                    MousePositionLabel.Text = "鼠标位置: 无法获取";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"鼠标位置更新错误: {ex.Message}");
                if (MousePositionLabel != null)
                {
                    MousePositionLabel.Text = $"鼠标位置: 错误 - {ex.Message}";
                }
            }
        }

        // 获取系统DPI缩放比例
        private double GetSystemDpiScale()
        {
            try
            {
                using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                {
                    return g.DpiX / 96.0; // 96 DPI是标准DPI
                }
            }
            catch
            {
                return 1.0; // 默认无缩放
            }
        }

        private void BlinkTimer_Tick(object sender, EventArgs e)
        {
            _blinkCount++;
            if (_blinkCount >= 15) // 3秒 = 15次 * 200ms
            {
                _blinkTimer.Stop();
                _blinkCount = 0;
                // 这里应该隐藏红框，但由于我们使用的是系统级绘制，
                // 实际实现可能需要更复杂的逻辑
            }
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F3:
                    // 只有在按钮可用时才响应热键
                    if (CaptureButton.IsEnabled)
                    {
                        CaptureButton_Click(sender, e);
                    }
                    break;
                case Key.F4:
                    // 只有在按钮可用时才响应热键
                    if (RecaptureButton.IsEnabled)
                    {
                        RecaptureButton_Click(sender, e);
                    }
                    break;
                case Key.F7:
                    // 只有在按钮可用时才响应热键
                    if (ValidateButton.IsEnabled)
                    {
                        ValidateButton_Click(sender, e);
                    }
                    break;
                case Key.Escape:
                    TrySetDialogResult(false);
                    this.Close();
                    break;
            }
        }

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前是否在检测Tab
            if (MainTabControl.SelectedItem == DetectionTab)
            {
                // 自动检测并截取窗口/元素
                AutoCaptureWindowOrElement();
            }
            else
            {
                // 手动截图模式
                ManualCapture();
            }
        }

        private void RecaptureButton_Click(object sender, RoutedEventArgs e)
        {
            // 检查当前是否在检测Tab
            if (MainTabControl.SelectedItem == DetectionTab)
            {
                // 自动检测并截取窗口/元素
                AutoCaptureWindowOrElement();
            }
            else
            {
                // 手动截图模式
                ManualCapture();
            }
        }

        /// <summary>
        /// 图片库按钮点击事件
        /// </summary>
        private void ImageLibraryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 打开图片库选择对话框
                var dialog = new MultiImageSelectorDialog();
                var result = dialog.ShowDialog();

                if (result == true && dialog.SelectedImagePaths.Count > 0)
                {
                    // 如果选择了多张图片,使用第一张
                    var selectedImagePath = dialog.SelectedImagePaths[0];

                    // 设置图像路径并加载
                    _imagePath = selectedImagePath;
                    LoadImage();

                    // 如果选择了多张,提示用户
                    if (dialog.SelectedImagePaths.Count > 1)
                    {
                        MessageBox.Show($"已选择 {dialog.SelectedImagePaths.Count} 张图片,当前仅加载第一张图片。\n支持多图识别功能正在开发中。",
                            "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开图片库失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 手动截图
        private void ManualCapture()
        {
            // 隐藏当前窗口
            this.Hide();

            // 打开截图窗口（传递 isRecaptureMode=true，避免打开新的编辑器窗口）
            var captureWindow = new CaptureWindow(isRecaptureMode: true);
            captureWindow.ShowDialog();

            // 如果截图成功，更新图像
            if (captureWindow.CaptureSucceeded && !string.IsNullOrEmpty(captureWindow.CapturedImagePath))
            {
                _imagePath = captureWindow.CapturedImagePath;
                LoadImage();

                // 切换到截图识别Tab
                MainTabControl.SelectedItem = ScreenshotTab;
            }

            // 重新显示当前窗口
            this.Show();
        }

        // 自动捕获窗口或元素
        private void AutoCaptureWindowOrElement()
        {
            try
            {
                // 获取当前鼠标位置
                GetCursorPos(out Point cursorPos);
                IntPtr hwnd = WindowFromPoint(cursorPos);

                if (hwnd == IntPtr.Zero)
                {
                    System.Windows.MessageBox.Show("未检测到窗口或元素！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取窗口矩形
                GetWindowRect(hwnd, out RECT rect);

                // 隐藏当前窗口
                this.Hide();

                // 等待窗口隐藏
                System.Threading.Thread.Sleep(200);

                // 截取窗口区域
                var screenshot = CaptureWindowArea(rect);

                if (screenshot != null)
                {
                    // 保存截图
                    var tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"autocapture_{DateTime.Now:yyyyMMddHHmmss}.png");
                    screenshot.Save(tempPath, System.Drawing.Imaging.ImageFormat.Png);

                    _imagePath = tempPath;
                    LoadImage();

                    // 获取窗口信息
                    var windowTitle = new System.Text.StringBuilder(256);
                    var className = new System.Text.StringBuilder(256);
                    GetWindowText(hwnd, windowTitle, 256);
                    GetClassName(hwnd, className, 256);

                    // 更新元素名称
                    var elementName = !string.IsNullOrEmpty(windowTitle.ToString())
                        ? windowTitle.ToString()
                        : className.ToString();

                    ElementNameTextBox.Text = elementName;

                    // 切换到截图识别Tab
                    MainTabControl.SelectedItem = ScreenshotTab;

                    System.Windows.MessageBox.Show(
                        $"已自动捕获:\n{elementName}\n位置: ({rect.Left}, {rect.Top})\n尺寸: {rect.Right - rect.Left}x{rect.Bottom - rect.Top}",
                        "捕获成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }

                // 重新显示当前窗口
                this.Show();
            }
            catch (Exception ex)
            {
                this.Show();
                System.Windows.MessageBox.Show($"自动捕获失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 截取窗口区域
        private System.Drawing.Bitmap CaptureWindowArea(RECT rect)
        {
            try
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                var bitmap = new System.Drawing.Bitmap(width, height);
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new System.Drawing.Size(width, height));
                }
                return bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"截图失败: {ex.Message}");
                return null;
            }
        }

        private void SaveImageButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 检查图像路径是否存在
                if (string.IsNullOrEmpty(_imagePath) || !File.Exists(_imagePath))
                {
                    System.Windows.MessageBox.Show("没有可保存的图像！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 获取图像信息
                var fileInfo = new FileInfo(_imagePath);
                var fileSizeInMB = fileInfo.Length / (1024.0 * 1024.0);
                const double SIZE_THRESHOLD_MB = 2.0;

                // 生成文件名（使用元素名称 + 时间戳）
                var elementName = ElementNameTextBox.Text.Trim();
                if (string.IsNullOrEmpty(elementName))
                {
                    elementName = "Image";
                }

                // 清理文件名中的非法字符
                var invalidChars = Path.GetInvalidFileNameChars();
                elementName = string.Join("_", elementName.Split(invalidChars));

                // 1. 保存到图像库
                using (var imageLibraryService = new ImageLibrary.Services.ImageLibraryService())
                {
                    var libraryItem = imageLibraryService.AddImage(
                        _imagePath,
                        elementName,
                        "截图",
                        $"保存于 {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                    );

                    string message = $"图像已保存到数据库！\n名称: {elementName}\n库ID: {libraryItem.Id}";
                    string additionalSavePath = null;

                    // 2. 如果文件大于2MB，额外保存到指定路径
                    if (fileSizeInMB > SIZE_THRESHOLD_MB)
                    {
                        if (!string.IsNullOrEmpty(_largeSavePath) && Directory.Exists(_largeSavePath))
                        {
                            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            var fileName = $"{elementName}_{timestamp}.png";
                            additionalSavePath = Path.Combine(_largeSavePath, fileName);

                            File.Copy(_imagePath, additionalSavePath, true);
                            message += $"\n\n文件大小 {fileSizeInMB:F2}MB > 2MB\n已额外保存到: {additionalSavePath}";
                        }
                        else
                        {
                            message += $"\n\n文件大小 {fileSizeInMB:F2}MB > 2MB\n但未设置大文件保存路径！";
                        }
                    }

                    // 显示成功消息
                    var result = System.Windows.MessageBox.Show(
                        message + (additionalSavePath != null ? "\n\n是否打开保存文件夹？" : ""),
                        "保存成功",
                        additionalSavePath != null ? MessageBoxButton.YesNo : MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes && additionalSavePath != null)
                    {
                        // 打开文件夹并选中文件
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{additionalSavePath}\"");
                    }

                    Console.WriteLine($"图像已保存: Name={elementName}");
                    if (additionalSavePath != null)
                    {
                        Console.WriteLine($"大文件已额外保存到: {additionalSavePath}");
                    }
                }

                // 保存成功后关闭窗口,返回到调用方
                TrySetDialogResult(true);
                this.Close();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"保存图像时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"保存图像错误: {ex.Message}");
            }
        }

        private void ValidateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 隐藏窗口以避免干扰屏幕截图
                this.Visibility = Visibility.Hidden;

                // 使用异步延迟来确保窗口完全隐藏后再进行截图
                var timer = new DispatcherTimer();
                timer.Interval = TimeSpan.FromMilliseconds(500); // 等待500ms确保窗口隐藏完成
                timer.Tick += (s, args) =>
                {
                    timer.Stop();
                    PerformValidation();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                // 恢复窗口可视状态
                this.Visibility = Visibility.Visible;
                // 显示错误状态
                ShowValidationStatus($"校验过程中发生错误: {ex.Message}", "Alert", System.Windows.Media.Colors.Red);
            }
        }

        private async void PerformValidation()
        {
            try
            {
                double threshold = SimilaritySlider.Value / 100.0;

                // 配置匹配参数
                var config = new ImageRecognitionConfig
                {
                    MatchThreshold = threshold,
                    UseGrayscale = true
                };

                // 使用IImageRecognitionService进行图像匹配
                var result = await _imageRecognitionService.FindImageOnScreenAsync(_imagePath, config);

                if (result != null && result.Confidence > 0)
                {
                    // 自动填入坐标到文本框（使用中心点坐标）
                    var xCoord = result.Location.X;
                    var yCoord = result.Location.Y;

                    // 使用Dispatcher确保UI更新在正确的线程上
                    this.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            // 确保文本框可编辑
                            MouseXTextBox.IsReadOnly = false;
                            MouseXTextBox.IsEnabled = true;
                            MouseYTextBox.IsReadOnly = false;
                            MouseYTextBox.IsEnabled = true;

                            // 设置坐标值
                            MouseXTextBox.Text = xCoord.ToString();
                            MouseYTextBox.Text = yCoord.ToString();

                            // 强制刷新UI
                            MouseXTextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateTarget();
                            MouseYTextBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateTarget();

                            Console.WriteLine($"坐标已设置: X={xCoord}, Y={yCoord}");
                            Console.WriteLine($"文本框内容: X={MouseXTextBox.Text}, Y={MouseYTextBox.Text}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"设置坐标时出错: {ex.Message}");
                        }
                    });

                    // 显示成功状态
                    ShowValidationStatus($"✓ 校验成功！找到图像位置: ({result.Location.X}, {result.Location.Y})，匹配度: {result.Confidence:P1}",
                                       "CheckCircle", System.Windows.Media.Colors.Green);

                    // 在匹配位置绘制闪烁框（传递左上角坐标和尺寸）
                    ShowBlinkingBox(result.Rectangle.X, result.Rectangle.Y, result.Rectangle.Width, result.Rectangle.Height, () =>
                    {
                        // 闪烁完成后恢复窗口可视性
                        this.Dispatcher.Invoke(() =>
                        {
                            this.Visibility = Visibility.Visible;
                            this.Activate(); // 激活窗口，使其获得焦点
                        });
                    });
                }
                else
                {
                    // 显示失败状态并恢复窗口可视性
                    ShowValidationStatus("✗ 校验失败！未能在屏幕上找到匹配的图像。请尝试调整识别相似度或重新捕获图像。",
                                       "AlertCircle", System.Windows.Media.Colors.Red);
                    this.Dispatcher.Invoke(() =>
                    {
                        this.Visibility = Visibility.Visible;
                    });
                }
            }
            catch (Exception ex)
            {
                // 恢复窗口可视状态
                this.Dispatcher.Invoke(() =>
                {
                    this.Visibility = Visibility.Visible;
                });
                // 显示错误状态
                ShowValidationStatus($"校验过程中发生错误: {ex.Message}", "Alert", System.Windows.Media.Colors.Red);
            }
        }

        /// <summary>
        /// 在指定位置显示闪烁的红框
        /// </summary>
        /// <param name="x">左上角X坐标（物理像素）</param>
        /// <param name="y">左上角Y坐标（物理像素）</param>
        /// <param name="width">宽度（物理像素）</param>
        /// <param name="height">高度（物理像素）</param>
        /// <param name="onComplete">完成回调</param>
        private void ShowBlinkingBox(double x, double y, int width, int height, Action onComplete)
        {
            try
            {
                // 使用NativeBlinkWindow显示闪烁框
                _currentBlinkWindow = new NativeBlinkWindow();
                _currentBlinkWindow.ShowBlinkAt(x, y, width, height, onComplete);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"显示闪烁框时出错: {ex.Message}");
                onComplete?.Invoke();
            }
        }

        private void ShowValidationStatus(string message, string iconKind, System.Windows.Media.Color color)
        {
            ValidationStatusPanel.Visibility = Visibility.Visible;
            ValidationStatusText.Text = message;

            // 设置图标
            if (ValidationStatusIcon != null)
            {
                switch (iconKind)
                {
                    case "CheckCircle":
                        ValidationStatusIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.CheckCircle;
                        break;
                    case "AlertCircle":
                        ValidationStatusIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.AlertCircle;
                        break;
                    case "Alert":
                        ValidationStatusIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Alert;
                        break;
                    default:
                        ValidationStatusIcon.Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Information;
                        break;
                }
                ValidationStatusIcon.Foreground = new System.Windows.Media.SolidColorBrush(color);
            }
        }



        private void SimilaritySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SimilarityValueText != null)
            {
                SimilarityValueText.Text = $"{(int)e.NewValue}%";
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            string helpText = @"📸 图像编辑器帮助 - AutoClicker Pro

🔧 功能说明：
• 重新捕获 (F4)：重新截取屏幕图像
• 校验图像 (F7)：验证当前图像是否能在屏幕上找到，找到后会在匹配位置显示闪烁红框
• 识别相似度：调整图像匹配的精确度 (0-100%)，数值越高要求越严格
• 鼠标位置控制：手动输入坐标或查看当前鼠标位置

⌨️ 快捷键：
• F4：重新捕获屏幕
• F7：校验图像匹配
• ESC：关闭窗口

📋 操作流程：
1. 设置有意义的元素名称
2. 根据需要调整识别相似度（建议70-90%）
3. 点击校验按钮验证图像识别效果
4. 校验成功后，会自动填入匹配位置坐标并显示闪烁红框
5. 确认无误后点击完成按钮

💡 提示：
• 校验时窗口会自动隐藏，避免干扰识别
• 校验成功会在匹配位置显示闪烁红框（约2秒）
• 可以使用鼠标位置控制功能测试坐标位置";

            System.Windows.MessageBox.Show(helpText, "图像编辑器帮助 - AutoClicker Pro", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveAndContinueButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTaskItem();
            // 这里可以添加保存到配置文件的逻辑
            System.Windows.MessageBox.Show("设置已保存，可以继续添加更多任务。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CompleteButton_Click(object sender, RoutedEventArgs e)
        {
            CreateTaskItem();
            IsCompleted = true;
            TrySetDialogResult(true);
            this.Close();
        }

        private void CreateTaskItem()
        {
            ResultTask = new TaskItem
            {
                Name = ElementNameTextBox.Text.Trim(),
                TemplateImagePath = _imagePath,
                Action = "click",
                SimilarityThreshold = SimilaritySlider.Value / 100.0,
                OnSuccess = "next",
                OnFail = "retry",
                RetryTimes = 3
            };
        }

        private void TrySetDialogResult(bool? value)
        {
            if (!ComponentDispatcher.IsThreadModal)
            {
                return;
            }

            try
            {
                DialogResult = value;
            }
            catch (InvalidOperationException)
            {
                // 忽略在非模态上下文中设置对话结果的请求
            }
        }

        private void MoveMouseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 从文本框获取坐标并移动鼠标到指定位置
                if (int.TryParse(MouseXTextBox.Text, out int x) && int.TryParse(MouseYTextBox.Text, out int y))
                {
                    // 使用Win32 API移动鼠标到指定位置（物理像素坐标）
                    if (SetCursorPos(x, y))
                    {
                        Console.WriteLine($"鼠标已移动到位置: ({x}, {y})");
                        // 鼠标跟踪Timer会自动更新显示，无需手动干预
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("移动鼠标失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    System.Windows.MessageBox.Show("请输入有效的坐标数字！", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"移动鼠标时发生错误: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImageEditorWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ComponentDispatcher.IsThreadModal)
            {
                return;
            }

            bool hasValue;
            try
            {
                hasValue = DialogResult.HasValue;
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (!hasValue)
            {
                TrySetDialogResult(false);
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // 停止计时器避免内存泄漏
            _blinkTimer?.Stop();
            _mouseTrackingTimer?.Stop();
            _detectionTimer?.Stop();

            // 清空闪烁窗口引用
            _currentBlinkWindow = null;

            base.OnClosed(e);
        }

        // Tab切换事件
        private void MainTabControl_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (MainTabControl.SelectedItem == DetectionTab)
            {
                Console.WriteLine("切换到窗口/元素检测Tab");
            }
            else if (MainTabControl.SelectedItem == ScreenshotTab)
            {
                Console.WriteLine("切换到截图识别Tab");
            }
        }

        // 开始检测按钮点击事件
        private void StartDetectionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedMode = DetectionModeComboBox.SelectedIndex;
                if (selectedMode == 0)
                {
                    // 窗口检测
                    StartWindowDetection();
                }
                else
                {
                    // UI元素检测
                    StartUIElementDetection();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"启动检测时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // 窗口检测
        private void StartWindowDetection()
        {
            if (_isDetecting)
            {
                StopDetection();
                return;
            }

            _isDetecting = true;
            var stopPanel1 = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            stopPanel1.Children.Add(new MahApps.Metro.IconPacks.PackIconMaterial { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Stop, Style = (Style)FindResource("Icon16Style") });
            stopPanel1.Children.Add(new TextBlock { Text = "停止检测" });
            StartDetectionButton.Content = stopPanel1;

            DetectionInfoText.Text = "窗口检测模式已启动...\n将鼠标移动到目标窗口上查看信息";

            // 创建检测计时器
            _detectionTimer = new DispatcherTimer();
            _detectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _detectionTimer.Tick += WindowDetectionTimer_Tick;
            _detectionTimer.Start();
        }

        // UI元素检测
        private void StartUIElementDetection()
        {
            if (_isDetecting)
            {
                StopDetection();
                return;
            }

            _isDetecting = true;
            var stopPanel2 = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            stopPanel2.Children.Add(new MahApps.Metro.IconPacks.PackIconMaterial { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Stop, Style = (Style)FindResource("Icon16Style") });
            stopPanel2.Children.Add(new TextBlock { Text = "停止检测" });
            StartDetectionButton.Content = stopPanel2;

            DetectionInfoText.Text = "UI元素检测模式已启动...\n将鼠标移动到目标UI元素上查看信息";

            // 创建检测计时器
            _detectionTimer = new DispatcherTimer();
            _detectionTimer.Interval = TimeSpan.FromMilliseconds(100);
            _detectionTimer.Tick += UIElementDetectionTimer_Tick;
            _detectionTimer.Start();
        }

        // 停止检测
        private void StopDetection()
        {
            _isDetecting = false;
            _detectionTimer?.Stop();
            _detectionTimer = null;

            var startPanel = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            startPanel.Children.Add(new MahApps.Metro.IconPacks.PackIconMaterial { Kind = MahApps.Metro.IconPacks.PackIconMaterialKind.Crosshairs, Style = (Style)FindResource("Icon16Style") });
            startPanel.Children.Add(new TextBlock { Text = "开始检测" });
            StartDetectionButton.Content = startPanel;

            DetectionInfoText.Text = "检测已停止";
        }

        // 窗口检测计时器
        private void WindowDetectionTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                GetCursorPos(out Point cursorPos);
                IntPtr hwnd = WindowFromPoint(cursorPos);

                if (hwnd != IntPtr.Zero)
                {
                    var windowTitle = new System.Text.StringBuilder(256);
                    var className = new System.Text.StringBuilder(256);
                    GetWindowText(hwnd, windowTitle, 256);
                    GetClassName(hwnd, className, 256);

                    GetWindowRect(hwnd, out RECT rect);

                    var info = $"窗口句柄: {hwnd}\n" +
                              $"窗口标题: {windowTitle}\n" +
                              $"窗口类名: {className}\n" +
                              $"窗口位置: ({rect.Left}, {rect.Top})\n" +
                              $"窗口尺寸: {rect.Right - rect.Left} x {rect.Bottom - rect.Top}\n" +
                              $"鼠标位置: ({cursorPos.X}, {cursorPos.Y})";

                    DetectionInfoText.Text = info;

                    // 更新预览图片
                    UpdateDetectionPreview(hwnd, rect);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"窗口检测错误: {ex.Message}");
            }
        }

        // UI元素检测计时器 - 使用 UI Automation API
        private void UIElementDetectionTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                GetCursorPos(out Point cursorPos);

                // 使用 UI Automation 获取鼠标位置下的元素
                var element = AutomationElement.FromPoint(new System.Windows.Point(cursorPos.X, cursorPos.Y));
                
                if (element != null)
                {
                    // 获取元素信息
                    var name = element.Current.Name;
                    var controlType = element.Current.ControlType.ProgrammaticName;
                    var className = element.Current.ClassName;
                    var automationId = element.Current.AutomationId;
                    var boundingRect = element.Current.BoundingRectangle;
                    var isEnabled = element.Current.IsEnabled;
                    var processId = element.Current.ProcessId;

                    // 获取父窗口信息
                    string parentInfo = "";
                    try
                    {
                        var parent = TreeWalker.ControlViewWalker.GetParent(element);
                        if (parent != null && parent != AutomationElement.RootElement)
                        {
                            parentInfo = $"\n父元素: {parent.Current.Name} ({parent.Current.ControlType.ProgrammaticName})";
                        }
                    }
                    catch { }

                    var info = $"元素名称: {(string.IsNullOrEmpty(name) ? "(无)" : name)}\n" +
                              $"控件类型: {controlType.Replace("ControlType.", "")}\n" +
                              $"类名: {(string.IsNullOrEmpty(className) ? "(无)" : className)}\n" +
                              $"自动化ID: {(string.IsNullOrEmpty(automationId) ? "(无)" : automationId)}\n" +
                              $"元素位置: ({(int)boundingRect.X}, {(int)boundingRect.Y})\n" +
                              $"元素尺寸: {(int)boundingRect.Width} x {(int)boundingRect.Height}\n" +
                              $"是否启用: {(isEnabled ? "是" : "否")}\n" +
                              $"进程ID: {processId}" +
                              parentInfo +
                              $"\n鼠标位置: ({cursorPos.X}, {cursorPos.Y})";

                    DetectionInfoText.Text = info;

                    // 更新预览图片 - 使用元素的边界矩形
                    if (!boundingRect.IsEmpty && boundingRect.Width > 0 && boundingRect.Height > 0)
                    {
                        var rect = new RECT
                        {
                            Left = (int)boundingRect.X,
                            Top = (int)boundingRect.Y,
                            Right = (int)(boundingRect.X + boundingRect.Width),
                            Bottom = (int)(boundingRect.Y + boundingRect.Height)
                        };
                        UpdateDetectionPreview(IntPtr.Zero, rect);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI元素检测错误: {ex.Message}");
                DetectionInfoText.Text = $"检测错误: {ex.Message}";
            }
        }

        /// <summary>
        /// 递归获取指定点下最深层的子窗口（用于窗口检测模式）
        /// </summary>
        private IntPtr GetDeepestChildWindow(IntPtr parent, Point screenPoint)
        {
            IntPtr current = parent;

            // 最多递归20层,避免无限循环
            for (int i = 0; i < 20; i++)
            {
                // 转换屏幕坐标到客户端坐标
                Point clientPoint = screenPoint;
                if (!ScreenToClient(current, ref clientPoint))
                    break;

                // 查找子窗口 (跳过不可见和禁用的控件)
                IntPtr child = ChildWindowFromPointEx(
                    current,
                    clientPoint,
                    CWP_SKIPINVISIBLE | CWP_SKIPDISABLED | CWP_SKIPTRANSPARENT
                );

                // 如果没有找到子窗口,或找到的就是当前窗口,说明已经到最深层了
                if (child == IntPtr.Zero || child == current)
                    break;

                current = child;
            }

            return current;
        }

        /// <summary>
        /// 更新检测预览图片 - 使用屏幕截图方式捕获元素区域
        /// </summary>
        private void UpdateDetectionPreview(IntPtr hwnd, RECT rect)
        {
            try
            {
                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                    return;

                // 限制最大尺寸以提高性能
                const int maxPreviewSize = 800;
                if (width > maxPreviewSize || height > maxPreviewSize)
                {
                    // 保持宽高比缩放
                    double scale = Math.Min((double)maxPreviewSize / width, (double)maxPreviewSize / height);
                    width = (int)(width * scale);
                    height = (int)(height * scale);
                }

                // 使用屏幕截图方式捕获指定区域（更可靠）
                using (var bitmap = new System.Drawing.Bitmap(rect.Right - rect.Left, rect.Bottom - rect.Top))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        // 设置高质量渲染
                        graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                        
                        // 从屏幕复制指定区域
                        graphics.CopyFromScreen(
                            rect.Left, rect.Top,
                            0, 0,
                            new System.Drawing.Size(rect.Right - rect.Left, rect.Bottom - rect.Top),
                            System.Drawing.CopyPixelOperation.SourceCopy);
                    }

                    // 转换为 WPF BitmapSource
                    IntPtr hBitmap = bitmap.GetHbitmap();
                    try
                    {
                        var bmpSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());

                        // 冻结以提高性能
                        bmpSource.Freeze();

                        // 更新预览图片
                        DetectionPreviewImage.Source = bmpSource;
                        DetectionPreviewText.Text = $"预览尺寸: {rect.Right - rect.Left} x {rect.Bottom - rect.Top} 像素";
                    }
                    finally
                    {
                        // 清理 HBITMAP
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新预览失败: {ex.Message}");
                DetectionPreviewText.Text = "预览生成失败";
            }
        }

        /// <summary>
        /// 加载保存路径配置
        /// </summary>
        private void LoadSavePath()
        {
            try
            {
                // XNode 项目中暂时不需要保存路径配置
                _largeSavePath = string.Empty;
                SavePathTextBox.Text = "未设置";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载保存路径配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存路径配置
        /// </summary>
        private void SavePathConfig()
        {
            try
            {
                // XNode 项目中暂时不需要保存路径配置
                // var settings = AutoClicker.Properties.Settings.Default;
                // settings.LargeSavePath = _largeSavePath;
                // settings.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存路径配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 设置保存路径按钮点击事件
        /// </summary>
        private void SetSavePathButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new System.Windows.Forms.FolderBrowserDialog
                {
                    Description = "选择大文件保存文件夹",
                    ShowNewFolderButton = true,
                    SelectedPath = !string.IsNullOrEmpty(_largeSavePath) ? _largeSavePath : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                };

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    _largeSavePath = dialog.SelectedPath;
                    SavePathTextBox.Text = _largeSavePath;
                    SavePathConfig();

                    System.Windows.MessageBox.Show(
                        $"大文件保存路径已设置为:\n{_largeSavePath}",
                        "设置成功",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);

                    Console.WriteLine($"大文件保存路径已设置: {_largeSavePath}");
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置保存路径失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine($"设置保存路径错误: {ex.Message}");
            }
        }

    }
}
