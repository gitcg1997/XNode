using System;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XNode.Windows.ImageLibrary.Models;

namespace XNode.Windows.ImageLibrary.ViewModels
{
    /// <summary>
    /// 图片项视图模型
    /// </summary>
    public partial class ImageItemViewModel : ObservableObject
    {
        private readonly ImageItem _model;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private BitmapImage? _thumbnail;

        public ImageItemViewModel(ImageItem model)
        {
            _model = model;
            LoadThumbnail();
        }

        public int Id => _model.Id;
        public string Name => _model.Name;
        public string? Description => _model.Description;
        public string CategoryName => _model.CategoryName;
        public string FilePath => _model.FilePath;
        public string? ThumbnailPath => _model.ThumbnailPath;
        public string FileSizeFormatted => _model.FileSizeFormatted;
        public string Resolution => _model.Resolution;
        public string RelativeTime => _model.RelativeTime;
        public bool IsFavorite => _model.IsFavorite;
        public int UsageCount => _model.UsageCount;
        public string Format => _model.Format;

        public ImageItem Model => _model;

        /// <summary>
        /// 加载缩略图
        /// </summary>
        private void LoadThumbnail()
        {
            try
            {
                var path = !string.IsNullOrEmpty(_model.ThumbnailPath) ? _model.ThumbnailPath : _model.FilePath;

                if (!System.IO.File.Exists(path))
                {
                    Thumbnail = null;
                    return;
                }

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(path, UriKind.Absolute);
                bitmap.DecodePixelWidth = 200; // 限制解码尺寸以节省内存
                bitmap.EndInit();
                bitmap.Freeze(); // 冻结以跨线程使用

                Thumbnail = bitmap;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载缩略图失败: {ex.Message}");
                Thumbnail = null;
            }
        }

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void Refresh()
        {
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
            OnPropertyChanged(nameof(CategoryName));
            OnPropertyChanged(nameof(IsFavorite));
            OnPropertyChanged(nameof(UsageCount));
            OnPropertyChanged(nameof(RelativeTime));
        }
    }
}
