using CommunityToolkit.Mvvm.ComponentModel;

namespace XNode.ViewModels
{
    /// <summary>
    /// 编辑器状态 - 集中管理编辑器的全局状态
    /// </summary>
    public partial class EditorState : ObservableObject
    {
        #region 单例

        private static EditorState? _instance;
        public static EditorState Instance => _instance ??= new EditorState();

        private EditorState() { }

        #endregion

        #region 项目状态

        /// <summary>
        /// 是否有打开的项目
        /// </summary>
        [ObservableProperty]
        private bool _isProjectOpen;

        /// <summary>
        /// 项目是否已修改
        /// </summary>
        [ObservableProperty]
        private bool _isProjectModified;

        /// <summary>
        /// 当前项目名称
        /// </summary>
        [ObservableProperty]
        private string _projectName = "";

        #endregion

        #region 命令状态

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        [ObservableProperty]
        private bool _canUndo;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        [ObservableProperty]
        private bool _canRedo;

        /// <summary>
        /// 撤销操作描述
        /// </summary>
        [ObservableProperty]
        private string _undoDescription = "";

        /// <summary>
        /// 重做操作描述
        /// </summary>
        [ObservableProperty]
        private string _redoDescription = "";

        #endregion

        #region 执行状态

        /// <summary>
        /// 是否正在执行节点图
        /// </summary>
        [ObservableProperty]
        private bool _isExecuting;

        /// <summary>
        /// 当前执行的节点名称
        /// </summary>
        [ObservableProperty]
        private string? _currentExecutingNode;

        #endregion

        #region 选择状态

        /// <summary>
        /// 选中的节点数量
        /// </summary>
        [ObservableProperty]
        private int _selectedNodeCount;

        /// <summary>
        /// 是否有选中的节点
        /// </summary>
        public bool HasSelectedNodes => SelectedNodeCount > 0;

        /// <summary>
        /// 是否选中了多个节点
        /// </summary>
        public bool HasMultipleSelectedNodes => SelectedNodeCount > 1;

        partial void OnSelectedNodeCountChanged(int value)
        {
            OnPropertyChanged(nameof(HasSelectedNodes));
            OnPropertyChanged(nameof(HasMultipleSelectedNodes));
        }

        #endregion

        #region UI 状态

        /// <summary>
        /// 日志区域是否可见
        /// </summary>
        [ObservableProperty]
        private bool _logAreaVisible = true;

        /// <summary>
        /// 属性面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _propertyPanelVisible = true;

        /// <summary>
        /// 节点库面板是否可见
        /// </summary>
        [ObservableProperty]
        private bool _nodeLibPanelVisible = true;

        #endregion

        #region 公开方法

        /// <summary>
        /// 更新命令状态
        /// </summary>
        public void UpdateCommandState(bool canUndo, bool canRedo, string undoDesc, string redoDesc)
        {
            CanUndo = canUndo;
            CanRedo = canRedo;
            UndoDescription = undoDesc;
            RedoDescription = redoDesc;
        }

        /// <summary>
        /// 更新项目状态
        /// </summary>
        public void UpdateProjectState(bool isOpen, bool isModified, string name)
        {
            IsProjectOpen = isOpen;
            IsProjectModified = isModified;
            ProjectName = name;
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        public void Reset()
        {
            IsProjectOpen = false;
            IsProjectModified = false;
            ProjectName = "";
            CanUndo = false;
            CanRedo = false;
            UndoDescription = "";
            RedoDescription = "";
            IsExecuting = false;
            CurrentExecutingNode = null;
            SelectedNodeCount = 0;
        }

        #endregion
    }
}
