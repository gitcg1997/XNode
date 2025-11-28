using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XNode.Windows.ImageLibrary.Models;
using XNode.Windows.ImageLibrary.Services;

namespace XNode.Windows.ImageLibrary.ViewModels
{
    /// <summary>
    /// 图片库主窗口视图模型
    /// </summary>
    public partial class ImageLibraryViewModel : ObservableObject, IDisposable
    {
        private readonly ImageLibraryService _service;

        [ObservableProperty]
        private ObservableCollection<CategoryViewModel> _categories = new();

        [ObservableProperty]
        private ObservableCollection<ImageItemViewModel> _images = new();

        [ObservableProperty]
        private ObservableCollection<ImageItemViewModel> _selectedImages = new();

        [ObservableProperty]
        private CategoryViewModel? _selectedCategory;

        [ObservableProperty]
        private ImageItemViewModel? _currentImage;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _statusText = "就绪";

        [ObservableProperty]
        private int _totalImages;

        [ObservableProperty]
        private int _selectedCount;

        [ObservableProperty]
        private ViewMode _currentViewMode = ViewMode.Grid;

        [ObservableProperty]
        private SortMode _currentSortMode = SortMode.DateDesc;

        [ObservableProperty]
        private bool _showFavoritesOnly;

        public ImageLibraryViewModel()
        {
            _service = new ImageLibraryService();
            InitializeAsync();
        }

        /// <summary>
        /// 初始化数据
        /// </summary>
        private async void InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadImagesAsync();
        }

        /// <summary>
        /// 加载分类
        /// </summary>
        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            await Task.Run(() =>
            {
                var categories = _service.GetAllCategories();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    Categories.Clear();
                    foreach (var category in categories)
                    {
                        Categories.Add(new CategoryViewModel(category));
                    }

                    // 默认选中"全部"
                    if (Categories.Count > 0)
                    {
                        SelectedCategory = Categories[0];
                        SelectedCategory.IsSelected = true;
                    }
                });
            });
        }

        /// <summary>
        /// 加载图片
        /// </summary>
        [RelayCommand]
        private async Task LoadImagesAsync()
        {
            IsLoading = true;
            StatusText = "正在加载图片...";

            try
            {
                await Task.Run(() =>
                {
                    List<ImageItem> items;

                    // 根据条件筛选
                    if (ShowFavoritesOnly)
                    {
                        items = _service.GetFavoriteImages();
                    }
                    else if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        items = _service.SearchImages(SearchText);
                    }
                    else if (SelectedCategory != null && SelectedCategory.Id > 0)
                    {
                        items = _service.GetImagesByCategory(SelectedCategory.Id);
                    }
                    else
                    {
                        items = _service.GetAllImages();
                    }

                    // 排序
                    items = SortImages(items);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Images.Clear();
                        foreach (var item in items)
                        {
                            Images.Add(new ImageItemViewModel(item));
                        }

                        TotalImages = Images.Count;
                        StatusText = $"共 {TotalImages} 张图片";
                    });
                });
            }
            catch (Exception ex)
            {
                StatusText = $"加载失败: {ex.Message}";
                MessageBox.Show($"加载图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadImagesAsync();
        }

        /// <summary>
        /// 清空搜索
        /// </summary>
        [RelayCommand]
        private async Task ClearSearchAsync()
        {
            SearchText = string.Empty;
            await LoadImagesAsync();
        }

        /// <summary>
        /// 选择分类
        /// </summary>
        [RelayCommand]
        private async Task SelectCategoryAsync(CategoryViewModel? category)
        {
            if (category == null) return;

            // 取消其他分类选中状态
            foreach (var cat in Categories)
            {
                cat.IsSelected = false;
            }

            category.IsSelected = true;
            SelectedCategory = category;
            await LoadImagesAsync();
        }

        /// <summary>
        /// 导入图片
        /// </summary>
        [RelayCommand]
        private async Task ImportImagesAsync()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "选择要导入的图片",
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                IsLoading = true;
                StatusText = $"正在导入 {dialog.FileNames.Length} 张图片...";

                try
                {
                    await Task.Run(() =>
                    {
                        var categoryName = SelectedCategory?.Name ?? "截图";
                        if (categoryName == "全部") categoryName = "截图";

                        _service.BatchImportImages(dialog.FileNames, categoryName);
                    });

                    await LoadCategoriesAsync();
                    await LoadImagesAsync();

                    StatusText = $"成功导入 {dialog.FileNames.Length} 张图片";
                }
                catch (Exception ex)
                {
                    StatusText = "导入失败";
                    MessageBox.Show($"导入图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }

        /// <summary>
        /// 删除选中的图片
        /// </summary>
        [RelayCommand]
        private async Task DeleteSelectedImagesAsync()
        {
            if (SelectedImages.Count == 0)
            {
                MessageBox.Show("请先选择要删除的图片", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"确定要删除选中的 {SelectedImages.Count} 张图片吗?\n此操作不可撤销!",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            IsLoading = true;
            StatusText = $"正在删除 {SelectedImages.Count} 张图片...";

            try
            {
                var ids = SelectedImages.Select(x => x.Id).ToList();

                await Task.Run(() =>
                {
                    _service.BatchDeleteImages(ids);
                });

                // 清空选择状态
                SelectedImages.Clear();
                SelectedCount = 0;
                CurrentImage = null;

                await LoadCategoriesAsync();
                await LoadImagesAsync();

                StatusText = $"成功删除 {ids.Count} 张图片";
            }
            catch (Exception ex)
            {
                StatusText = "删除失败";
                MessageBox.Show($"删除图片失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavoriteAsync(ImageItemViewModel? image)
        {
            if (image == null) return;

            try
            {
                await Task.Run(() =>
                {
                    _service.ToggleFavorite(image.Id);
                });

                image.Refresh();
                OnPropertyChanged(nameof(Images));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 切换视图模式
        /// </summary>
        [RelayCommand]
        private void SwitchViewMode(ViewMode mode)
        {
            CurrentViewMode = mode;
        }

        /// <summary>
        /// 切换排序模式
        /// </summary>
        [RelayCommand]
        private async Task ChangeSortModeAsync(SortMode mode)
        {
            CurrentSortMode = mode;
            await LoadImagesAsync();
        }

        /// <summary>
        /// 切换只显示收藏
        /// </summary>
        [RelayCommand]
        private async Task ToggleFavoritesOnlyAsync()
        {
            ShowFavoritesOnly = !ShowFavoritesOnly;
            await LoadImagesAsync();
        }

        /// <summary>
        /// 全选
        /// </summary>
        [RelayCommand]
        private void SelectAll()
        {
            SelectedImages.Clear();
            foreach (var image in Images)
            {
                image.IsSelected = true;
                SelectedImages.Add(image);
            }
            SelectedCount = SelectedImages.Count;
        }

        /// <summary>
        /// 取消全选
        /// </summary>
        [RelayCommand]
        private void ClearSelection()
        {
            foreach (var image in SelectedImages)
            {
                image.IsSelected = false;
            }
            SelectedImages.Clear();
            SelectedCount = 0;
        }

        /// <summary>
        /// 图片选择变化
        /// </summary>
        public void OnImageSelectionChanged(ImageItemViewModel image, bool isSelected)
        {
            if (isSelected)
            {
                if (!SelectedImages.Contains(image))
                {
                    SelectedImages.Add(image);
                }
            }
            else
            {
                SelectedImages.Remove(image);
            }

            SelectedCount = SelectedImages.Count;
        }

        /// <summary>
        /// 选择图片
        /// </summary>
        [RelayCommand]
        private void SelectImage(ImageItemViewModel? image)
        {
            if (image == null) return;

            // 切换选中状态
            image.IsSelected = !image.IsSelected;
            OnImageSelectionChanged(image, image.IsSelected);

            // 设置为当前图片
            if (image.IsSelected)
            {
                CurrentImage = image;
            }
        }

        /// <summary>
        /// 在资源管理器中打开
        /// </summary>
        [RelayCommand]
        private void OpenInExplorer(ImageItemViewModel? image)
        {
            if (image == null) return;

            try
            {
                if (System.IO.File.Exists(image.FilePath))
                {
                    System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{image.FilePath}\"");
                }
                else
                {
                    MessageBox.Show("文件不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 排序图片
        /// </summary>
        private List<ImageItem> SortImages(List<ImageItem> items)
        {
            return CurrentSortMode switch
            {
                SortMode.DateDesc => items.OrderByDescending(x => x.CreatedAt).ToList(),
                SortMode.DateAsc => items.OrderBy(x => x.CreatedAt).ToList(),
                SortMode.NameAsc => items.OrderBy(x => x.Name).ToList(),
                SortMode.NameDesc => items.OrderByDescending(x => x.Name).ToList(),
                SortMode.SizeDesc => items.OrderByDescending(x => x.FileSize).ToList(),
                SortMode.SizeAsc => items.OrderBy(x => x.FileSize).ToList(),
                SortMode.UsageDesc => items.OrderByDescending(x => x.UsageCount).ToList(),
                _ => items
            };
        }

        /// <summary>
        /// 刷新
        /// </summary>
        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadCategoriesAsync();
            await LoadImagesAsync();
        }

        public void Dispose()
        {
            _service?.Dispose();
        }
    }

    /// <summary>
    /// 视图模式
    /// </summary>
    public enum ViewMode
    {
        Grid,   // 网格
        List    // 列表
    }

    /// <summary>
    /// 排序模式
    /// </summary>
    public enum SortMode
    {
        DateDesc,    // 日期降序
        DateAsc,     // 日期升序
        NameAsc,     // 名称升序
        NameDesc,    // 名称降序
        SizeDesc,    // 大小降序
        SizeAsc,     // 大小升序
        UsageDesc    // 使用次数降序
    }
}
