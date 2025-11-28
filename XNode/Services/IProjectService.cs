namespace XNode.Services
{
    /// <summary>
    /// 项目服务接口
    /// </summary>
    public interface IProjectService
    {
        /// <summary>
        /// 当前项目名称
        /// </summary>
        string? CurrentProjectName { get; }

        /// <summary>
        /// 是否有打开的项目
        /// </summary>
        bool HasProject { get; }

        /// <summary>
        /// 项目是否已保存
        /// </summary>
        bool IsSaved { get; }

        /// <summary>
        /// 新建项目
        /// </summary>
        void NewProject();

        /// <summary>
        /// 打开项目
        /// </summary>
        /// <returns>是否成功打开</returns>
        bool OpenProject();

        /// <summary>
        /// 保存项目
        /// </summary>
        /// <returns>是否成功保存</returns>
        bool SaveProject();

        /// <summary>
        /// 另存为项目
        /// </summary>
        /// <returns>是否成功保存</returns>
        bool SaveAsProject();

        /// <summary>
        /// 关闭项目
        /// </summary>
        void CloseProject();

        /// <summary>
        /// 项目状态变更事件
        /// </summary>
        event Action? ProjectStateChanged;
    }
}
