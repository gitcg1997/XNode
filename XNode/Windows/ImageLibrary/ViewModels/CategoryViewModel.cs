using CommunityToolkit.Mvvm.ComponentModel;
using XNode.Windows.ImageLibrary.Models;

namespace XNode.Windows.ImageLibrary.ViewModels
{
    /// <summary>
    /// 分类视图模型
    /// </summary>
    public partial class CategoryViewModel : ObservableObject
    {
        private readonly Category _model;

        [ObservableProperty]
        private bool _isSelected;

        [ObservableProperty]
        private int _imageCount;

        public CategoryViewModel(Category model)
        {
            _model = model;
            _imageCount = model.ImageCount;
        }

        public int Id => _model.Id;
        public string Name => _model.Name;
        public string? Description => _model.Description;
        public string? Icon => _model.Icon;
        public string Color => _model.Color;
        public bool IsSystem => _model.IsSystem;
        public int SortOrder => _model.SortOrder;

        public Category Model => _model;

        /// <summary>
        /// 刷新显示
        /// </summary>
        public void Refresh()
        {
            ImageCount = _model.ImageCount;
            OnPropertyChanged(nameof(Name));
            OnPropertyChanged(nameof(Description));
        }
    }
}
