using XNode.SubSystem.ProjectSystem;

namespace XNode.Services
{
    /// <summary>
    /// 项目服务实现 - 包装现有的 ProjectManager
    /// </summary>
    public class ProjectService : IProjectService
    {
        #region 单例

        private static ProjectService? _instance;
        public static ProjectService Instance => _instance ??= new ProjectService();

        private ProjectService()
        {
            // 订阅 ProjectManager 的事件
            SubSystem.EventSystem.EM.Instance.Add(
                SubSystem.EventSystem.EventType.Project_Changed,
                () => ProjectStateChanged?.Invoke()
            );
        }

        #endregion

        #region IProjectService 实现

        public string? CurrentProjectName => ProjectManager.Instance.CurrentProject?.ProjectName;

        public bool HasProject => ProjectManager.Instance.CurrentProject != null;

        public bool IsSaved => ProjectManager.Instance.Saved;

        public void NewProject()
        {
            ProjectManager.Instance.NewProject();
            ProjectStateChanged?.Invoke();
        }

        public bool OpenProject()
        {
            ProjectManager.Instance.OpenProject();
            ProjectStateChanged?.Invoke();
            return HasProject;
        }

        public bool SaveProject()
        {
            ProjectManager.Instance.SaveProject();
            bool result = ProjectManager.Instance.Saved;
            ProjectStateChanged?.Invoke();
            return result;
        }

        public bool SaveAsProject()
        {
            ProjectManager.Instance.SaveAsProject();
            bool result = ProjectManager.Instance.Saved;
            ProjectStateChanged?.Invoke();
            return result;
        }

        public void CloseProject()
        {
            ProjectManager.Instance.CloseProject();
            ProjectStateChanged?.Invoke();
        }

        public event Action? ProjectStateChanged;

        #endregion
    }
}
