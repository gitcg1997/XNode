using XNode.SubSystem.EventSystem;

namespace XNode.Command
{
    /// <summary>
    /// 命令抽象基类，提供通用功能和模板方法
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        /// <summary>
        /// 命令描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 执行命令
        /// </summary>
        public abstract void Execute();

        /// <summary>
        /// 撤销命令
        /// </summary>
        public abstract void Undo();

        /// <summary>
        /// 重做命令（默认实现为重新执行）
        /// </summary>
        public virtual void Redo()
        {
            Execute();
        }

        /// <summary>
        /// 通知UI更新 - 统一的UI更新入口
        /// </summary>
        protected void NotifyUIUpdate()
        {
            try
            {
                // 通过项目变更事件通知UI更新
                EM.Instance.Invoke(EventType.Project_Changed);
                LogInfo("已请求更新UI");
            }
            catch (Exception ex)
            {
                LogError($"更新UI失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        /// <param name="message">日志消息</param>
        protected void LogInfo(string message)
        {
            MainWindow.LogManager.LogInfo(message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        /// <param name="message">日志消息</param>
        protected void LogWarning(string message)
        {
            MainWindow.LogManager.LogWarning(message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        protected void LogError(string message)
        {
            MainWindow.LogManager.LogError(message);
        }
    }
}
