using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.ResourceSystem;
using XNode.Windows.ImageLibrary.Views;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace XNode.SubSystem.NodeEditSystem.Control
{
    /// <summary>
    /// 图像路径引脚组视图 - 带有浏览、图像库按钮和悬停预览
    /// </summary>
    public partial class ImagePathPinGroupView : PinGroupViewBase
    {
        #region 属性

        public ImagePathPinGroup? Instance { get; set; }

        #endregion

        #region 字段

        private CancellationTokenSource? _popupImageLoadCts;
        private static readonly Dictionary<string, BitmapSource> _imageCache = new();
        private static readonly object _cacheLock = new();
        private DispatcherTimer? _hoverTimer;
        private DispatcherTimer? _closeTimer;
        private const int HOVER_DELAY_MS = 300;
        private const int CLOSE_DELAY_MS = 50;

        #endregion

        #region 构造函数

        public ImagePathPinGroupView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            _hoverTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(HOVER_DELAY_MS) };
            _hoverTimer.Tick += HoverTimer_Tick;

            _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(CLOSE_DELAY_MS) };
            _closeTimer.Tick += CloseTimer_Tick;

            HoverPreviewPopup.MouseEnter += HoverPreviewPopup_MouseEnter;
            HoverPreviewPopup.MouseLeave += HoverPreviewPopup_MouseLeave;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) { }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _popupImageLoadCts?.Cancel();
            _popupImageLoadCts?.Dispose();
            _hoverTimer?.Stop();
            _hoverTimer = null;
            _closeTimer?.Stop();
            _closeTimer = null;
        }

        #endregion

        #region 基类方法

        public override void Init()
        {
            if (Instance == null) return;

            Block_Name.Text = Instance.Name;
            Block_Name.Foreground = new SolidColorBrush(GetDataPinColor());
            Input_Value.Text = Instance.Value;
            Input_Value.IsReadOnly = !Instance.CanInput;
            InputBoxArea.Width = new GridLength(Instance.BoxWidth);

            if (Instance.InputPin == null)
            {
                Icon_LeftPin.Visibility = Visibility.Collapsed;
                LeftPinArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                Icon_LeftPin.Source = GetDataPinIcon();
                LeftPinArea.MouseEnter += LeftPinArea_MouseEnter;
                LeftPinArea.MouseLeave += PinArea_MouseLeave;
            }

            if (Instance.OutputPin == null)
            {
                Icon_RightPin.Visibility = Visibility.Collapsed;
                RightPinArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                Icon_RightPin.Source = GetDataPinIcon();
                RightPinArea.MouseEnter += RightPinArea_MouseEnter;
                RightPinArea.MouseLeave += PinArea_MouseLeave;
            }

            Instance.ValueChanged += ValueChanged;
            Input_Value.TextChanged += Input_Value_TextChanged;
        }

        public override Grid GetPinArea()
        {
            if (Instance?.InputPin != null && HoveredPin == Instance.InputPin) return LeftPinArea;
            if (Instance?.OutputPin != null && HoveredPin == Instance.OutputPin) return RightPinArea;
            throw new Exception("无命中引脚");
        }

        public override WpfPoint GetPinOffset(NodeView card, int pinIndex)
        {
            if (pinIndex == 0) return LeftPinArea.TranslatePoint(new WpfPoint(3, 8), card);
            return RightPinArea.TranslatePoint(new WpfPoint(14, 8), card);
        }

        public override void UpdatePinIcon()
        {
            if (Instance?.InputPin != null)
                Icon_LeftPin.Source = GetDataPinIcon(Instance.InputPin.SourceList.Count > 0);
            if (Instance?.OutputPin != null)
                Icon_RightPin.Source = GetDataPinIcon(Instance.OutputPin.TargetList.Count > 0);
        }

        #endregion

        #region 按钮事件

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new WpfOpenFileDialog
                {
                    Title = "选择模板图像",
                    Filter = "图像文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                    Multiselect = false
                };

                if (dialog.ShowDialog() == true)
                {
                    if (Instance != null)
                    {
                        Instance.SetValue(dialog.FileName);
                        Input_Value.Text = dialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"浏览文件时发生错误: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var imageLibraryWindow = new ImageLibraryWindow();
                imageLibraryWindow.Owner = Window.GetWindow(this);
                
                if (imageLibraryWindow.ShowDialog() == true)
                {
                    var selectedPath = imageLibraryWindow.SelectedImagePath;
                    if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
                    {
                        if (Instance != null)
                        {
                            Instance.SetValue(selectedPath);
                            Input_Value.Text = selectedPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开图像库时发生错误: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region 引脚事件

        private void LeftPinArea_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = Instance?.InputPin;
        }

        private void RightPinArea_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = Instance?.OutputPin;
        }

        private void PinArea_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = null;
        }

        #endregion

        #region 私有方法

        private WpfColor GetDataPinColor()
        {
            return Instance?.Type switch
            {
                "int" => PinColorSet.Int,
                "double" => PinColorSet.Double,
                "string" => PinColorSet.String,
                "bool" => PinColorSet.Bool,
                "byte[]" => PinColorSet.ByteArray,
                _ => Colors.White,
            };
        }

        private BitmapSource? GetDataPinIcon(bool solid = false)
        {
            if (Instance == null) return null;

            return Instance.Type switch
            {
                "int" or "double" or "string" or "bool" or "byte[]" =>
                    PinIconManager.Instance.GetDataPinIcon(Instance.Type, solid),
                _ => null,
            };
        }

        private void ValueChanged()
        {
            Dispatcher.Invoke(() =>
            {
                if (Instance != null)
                    Input_Value.Text = Instance.Value;
            });
        }

        private void Input_Value_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Instance != null)
                Instance.SetValue(Input_Value.Text);
        }

        #endregion

        #region 悬停预览

        private void PreviewIcon_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            if (Instance == null) return;

            var imagePath = Instance.Value;
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return;

            _closeTimer?.Stop();
            _hoverTimer?.Stop();
            _hoverTimer?.Start();
        }

        private void PreviewIcon_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            _hoverTimer?.Stop();

            if (HoverPreviewPopup.IsOpen)
            {
                _closeTimer?.Stop();
                _closeTimer?.Start();
            }
        }

        private void HoverPreviewPopup_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            _closeTimer?.Stop();
        }

        private void HoverPreviewPopup_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            _closeTimer?.Stop();
            _closeTimer?.Start();
        }

        private void CloseTimer_Tick(object? sender, EventArgs e)
        {
            _closeTimer?.Stop();

            if (PreviewIconButton.IsMouseOver || HoverPreviewPopup.IsMouseOver)
                return;

            HoverPreviewPopup.IsOpen = false;
        }

        private void HoverTimer_Tick(object? sender, EventArgs e)
        {
            _hoverTimer?.Stop();

            if (Instance == null) return;

            var imagePath = Instance.Value;
            if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                return;

            ShowHoverPreview(imagePath);
        }

        private async void ShowHoverPreview(string imagePath)
        {
            try
            {
                _popupImageLoadCts?.Cancel();
                _popupImageLoadCts = new CancellationTokenSource();

                HoverPreviewPopup.IsOpen = true;

                PopupLoadingPanel.Visibility = Visibility.Visible;
                PopupPreviewImage.Visibility = Visibility.Collapsed;
                PopupErrorText.Visibility = Visibility.Collapsed;
                PopupImageInfo.Text = "";
                PopupFileSizeInfo.Text = "";

                BitmapSource? cachedImage = null;
                lock (_cacheLock)
                {
                    _imageCache.TryGetValue(imagePath, out cachedImage);
                }

                if (cachedImage != null)
                {
                    ShowPopupImage(cachedImage, imagePath);
                    return;
                }

                var image = await Task.Run(() => LoadImageFromFile(imagePath), _popupImageLoadCts.Token);

                if (!_popupImageLoadCts.Token.IsCancellationRequested && image != null)
                {
                    lock (_cacheLock)
                    {
                        if (!_imageCache.ContainsKey(imagePath))
                            _imageCache[imagePath] = image;
                    }
                    ShowPopupImage(image, imagePath);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                PopupLoadingPanel.Visibility = Visibility.Collapsed;
                PopupPreviewImage.Visibility = Visibility.Collapsed;
                PopupErrorText.Visibility = Visibility.Visible;
                PopupErrorText.Text = $"加载失败: {ex.Message}";
            }
        }

        private BitmapSource? LoadImageFromFile(string imagePath)
        {
            try
            {
                using var stream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var bitmap = decoder.Frames[0];

                BitmapSource result;

                if (bitmap.Width > 380 || bitmap.Height > 280)
                {
                    var scaleX = 380.0 / bitmap.Width;
                    var scaleY = 280.0 / bitmap.Height;
                    var scale = Math.Min(scaleX, scaleY);

                    var transformedBitmap = new TransformedBitmap(bitmap, new ScaleTransform(scale, scale));
                    var writableBitmap = new WriteableBitmap(transformedBitmap);
                    writableBitmap.Freeze();
                    result = writableBitmap;
                }
                else
                {
                    var writableBitmap = new WriteableBitmap(bitmap);
                    writableBitmap.Freeze();
                    result = writableBitmap;
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"加载图像失败: {ex.Message}", ex);
            }
        }

        private void ShowPopupImage(BitmapSource image, string imagePath)
        {
            PopupLoadingPanel.Visibility = Visibility.Collapsed;
            PopupPreviewImage.Visibility = Visibility.Visible;
            PopupErrorText.Visibility = Visibility.Collapsed;
            PopupPreviewImage.Source = image;

            PopupImageInfo.Text = $"{image.PixelWidth} x {image.PixelHeight}";

            try
            {
                var fileInfo = new FileInfo(imagePath);
                var sizeKB = fileInfo.Length / 1024.0;
                PopupFileSizeInfo.Text = sizeKB < 1024 ? $"{sizeKB:F1} KB" : $"{sizeKB / 1024.0:F1} MB";
            }
            catch
            {
                PopupFileSizeInfo.Text = "";
            }
        }

        #endregion
    }
}
