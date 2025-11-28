using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using XNode.Windows.ImageEditor.Models;
using XNode.Windows.ImageEditor.Services;

namespace XNode.Windows.ImageEditor
{
    /// <summary>
    /// 多选图像选择器对话框
    /// 支持从图片库中选择多张图片作为模板
    /// </summary>
    public partial class MultiImageSelectorDialog : Window
    {
        #region 字段

        private readonly ImageLibraryService _imageLibraryService;
        private List<ImageLibraryItem> _allImages = new List<ImageLibraryItem>();

        #endregion

        #region 属性

        /// <summary>
        /// 选择的图片路径列表
        /// </summary>
        public List<string> SelectedImagePaths { get; private set; } = new List<string>();

        /// <summary>
        /// 选择的图片库项目列表
        /// </summary>
        public List<ImageLibraryItem> SelectedLibraryImages { get; private set; } = new List<ImageLibraryItem>();

        #endregion

        #region 构造函数

        public MultiImageSelectorDialog()
        {
            InitializeComponent();
            _imageLibraryService = new ImageLibraryService();

            Loaded += MultiImageSelectorDialog_Loaded;
        }

        #endregion

        #region 事件处理

        /// <summary>
        /// 窗口加载完成
        /// </summary>
        private void MultiImageSelectorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLibraryImages();
        }

        /// <summary>
        /// 图片库列表选择改变
        /// </summary>
        private void LibraryImagesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionCount();
        }

        /// <summary>
        /// 搜索框文本改变
        /// </summary>
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                DisplayLibraryImages(_allImages);
            }
            else
            {
                try
                {
                    var results = _imageLibraryService.SearchImages(searchTerm);
                    DisplayLibraryImages(results);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"搜索失败: {ex.Message}", "错误",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// 全选按钮点击
        /// </summary>
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            LibraryImagesListBox.SelectAll();
            UpdateSelectionCount();
        }

        /// <summary>
        /// 清空选择按钮点击
        /// </summary>
        private void ClearSelectionButton_Click(object sender, RoutedEventArgs e)
        {
            LibraryImagesListBox.SelectedItems.Clear();
            UpdateSelectionCount();
        }

        /// <summary>
        /// 确定按钮点击
        /// </summary>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            if (LibraryImagesListBox.SelectedItems.Count == 0)
            {
                MessageBox.Show("请至少选择一张图片", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 收集选择的图片
            SelectedLibraryImages = LibraryImagesListBox.SelectedItems
                .Cast<ImageLibraryItem>()
                .ToList();

            SelectedImagePaths = SelectedLibraryImages
                .Select(item => item.FilePath)
                .ToList();

            DialogResult = true;
            Close();
        }

        /// <summary>
        /// 取消按钮点击
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 加载图片库图片
        /// </summary>
        private void LoadLibraryImages()
        {
            try
            {
                _allImages = _imageLibraryService.GetAllImages();
                DisplayLibraryImages(_allImages);
                UpdateSelectionCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载图片库失败: {ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示图片库图片
        /// </summary>
        private void DisplayLibraryImages(List<ImageLibraryItem> images)
        {
            LibraryImagesListBox.Items.Clear();

            foreach (var image in images)
            {
                LibraryImagesListBox.Items.Add(image);
            }
        }

        /// <summary>
        /// 更新选择数量显示
        /// </summary>
        private void UpdateSelectionCount()
        {
            int count = LibraryImagesListBox.SelectedItems.Count;

            if (count == 0)
            {
                SelectionCountText.Text = "未选择图片";
            }
            else if (count == 1)
            {
                var selectedItem = LibraryImagesListBox.SelectedItems[0] as ImageLibraryItem;
                SelectionCountText.Text = $"已选择: {selectedItem?.Name}";
            }
            else
            {
                SelectionCountText.Text = $"已选择 {count} 张图片";
            }
        }

        #endregion
    }
}
