using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using XNode.Windows.ImageLibrary.ViewModels;
using XNode.Windows.ImageLibrary.Services;

namespace XNode.Windows.ImageLibrary.Views
{
    /// <summary>
    /// 图片库主窗口
    /// </summary>
    public partial class ImageLibraryWindow : Window
    {
        private readonly ImageLibraryViewModel _viewModel;
        private readonly ImageLibraryService _service;

        public List<string> SelectedImagePaths { get; private set; } = new();

        /// <summary>
        /// 获取第一个选中的图像路径 (用于单选模式)
        /// </summary>
        public string? SelectedImagePath => SelectedImagePaths.FirstOrDefault();

        public ImageLibraryWindow()
        {
            InitializeComponent();

            _viewModel = new ImageLibraryViewModel();
            _service = new ImageLibraryService();
            DataContext = _viewModel;

            Closing += ImageLibraryWindow_Closing;
        }

        private void ImageLibraryWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Dispose();
            _service?.Dispose();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取选中的图片路径
            SelectedImagePaths = _viewModel.SelectedImages
                .Select(x => x.FilePath)
                .ToList();

            // 增加使用次数
            foreach (var image in _viewModel.SelectedImages)
            {
                try
                {
                    _service.IncrementUsageCount(image.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"增加使用次数失败: {ex.Message}");
                }
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
