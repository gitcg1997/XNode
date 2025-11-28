using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using XNode.SubSystem.ProjectSystem;
using XNode.SubSystem.WindowSystem;
using XNode.AppTool;

namespace XNode.ViewModels
{
    /// <summary>
    /// 主窗口 ViewModel
    /// </summary>
    public partial class MainWindowViewModel : ViewModelBase
    {
        #region 属性

        /// <summary>
        /// 窗口标题
        /// </summary>
        [ObservableProperty]
        private string _windowTitle = AppDelegate.AppTitle;

        /// <summary>
        /// 是否可以撤销
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
        private bool _canUndo;

        /// <summary>
        /// 是否可以重做
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RedoCommand))]
        private bool _canRedo;

        /// <summary>
        /// 日志区域是否可见
        /// </summary>
        [ObservableProperty]
        private bool _logAreaVisible = true;

        /// <summary>
        /// 是否正在执行节点图
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        [NotifyCanExecuteChangedFor(nameof(StopCommand))]
        private bool _isExecuting;

        /// <summary>
        /// 是否有打开的项目
        /// </summary>
        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SaveProjectCommand))]
        [NotifyCanExecuteChangedFor(nameof(SaveAsCommand))]
        [NotifyCanExecuteChangedFor(nameof(RunCommand))]
        private bool _hasProject;

        #endregion

        #region 命令

        /// <summary>
        /// 新建项目命令
        /// </summary>
        [RelayCommand]
        private void NewProject()
        {
            ProjectManager.Instance.NewProject();
            UpdateWindowTitle();
            UpdateProjectState();
        }

        /// <summary>
        /// 打开项目命令
        /// </summary>
        [RelayCommand]
        private void OpenProject()
        {
            ProjectManager.Instance.OpenProject();
            UpdateWindowTitle();
            UpdateProjectState();
        }

        /// <summary>
        /// 保存项目命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveProject))]
        private void SaveProject()
        {
            ProjectManager.Instance.SaveProject();
            UpdateWindowTitle();
        }

        private bool CanSaveProject() => HasProject;

        /// <summary>
        /// 另存为命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSaveAs))]
        private void SaveAs()
        {
            ProjectManager.Instance.SaveAsProject();
            UpdateWindowTitle();
        }

        private bool CanSaveAs() => HasProject;

        /// <summary>
        /// 撤销命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteUndo))]
        private void Undo()
        {
            // 实际撤销逻辑在 MainWindow 中调用 CommandManager
            OnUndoRequested?.Invoke();
        }

        private bool CanExecuteUndo() => CanUndo;

        /// <summary>
        /// 重做命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExecuteRedo))]
        private void Redo()
        {
            // 实际重做逻辑在 MainWindow 中调用 CommandManager
            OnRedoRequested?.Invoke();
        }

        private bool CanExecuteRedo() => CanRedo;

        /// <summary>
        /// 运行命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanRun))]
        private void Run()
        {
            OnRunRequested?.Invoke();
        }

        private bool CanRun() => HasProject && !IsExecuting;

        /// <summary>
        /// 停止命令
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanStop))]
        private void Stop()
        {
            OnStopRequested?.Invoke();
        }

        private bool CanStop() => IsExecuting;

        /// <summary>
        /// 切换日志区域显示
        /// </summary>
        [RelayCommand]
        private void ToggleLogArea()
        {
            LogAreaVisible = !LogAreaVisible;
        }

        /// <summary>
        /// 清空日志
        /// </summary>
        [RelayCommand]
        private void ClearLog()
        {
            OnClearLogRequested?.Invoke();
        }

        #endregion

        #region 事件

        /// <summary>
        /// 请求撤销事件
        /// </summary>
        public event Action? OnUndoRequested;

        /// <summary>
        /// 请求重做事件
        /// </summary>
        public event Action? OnRedoRequested;

        /// <summary>
        /// 请求运行事件
        /// </summary>
        public event Action? OnRunRequested;

        /// <summary>
        /// 请求停止事件
        /// </summary>
        public event Action? OnStopRequested;

        /// <summary>
        /// 请求清空日志事件
        /// </summary>
        public event Action? OnClearLogRequested;

        #endregion

        #region 公开方法

        /// <summary>
        /// 更新窗口标题
        /// </summary>
        public void UpdateWindowTitle()
        {
            if (ProjectManager.Instance.CurrentProject != null)
            {
                WindowTitle = ProjectManager.Instance.CurrentProject.ProjectName;
                if (!ProjectManager.Instance.Saved) WindowTitle += "*";
                WindowTitle += " - " + AppDelegate.AppTitle;
            }
            else
            {
                WindowTitle = AppDelegate.AppTitle;
            }
        }

        /// <summary>
        /// 更新撤销/重做状态
        /// </summary>
        public void UpdateUndoRedoState(bool canUndo, bool canRedo)
        {
            CanUndo = canUndo;
            CanRedo = canRedo;
        }

        /// <summary>
        /// 更新项目状态
        /// </summary>
        public void UpdateProjectState()
        {
            HasProject = ProjectManager.Instance.CurrentProject != null;
        }

        /// <summary>
        /// 更新执行状态
        /// </summary>
        public void UpdateExecutionState(bool isExecuting)
        {
            IsExecuting = isExecuting;
        }

        #endregion
    }
}
