namespace XNode.Command
{
    /// <summary>
    /// 添加节点位置确认拦截器
    /// </summary>
    /// <remarks>
    /// 负责处理 AddNodeCommand 的待确认位置逻辑：
    /// - 当执行非移动命令时，自动确认上一个添加节点的位置
    /// - 清除拖放标志
    /// </remarks>
    public class AddNodePositionInterceptor : ICommandInterceptor
    {
        private readonly CommandManager _commandManager;

        public AddNodePositionInterceptor(CommandManager commandManager)
        {
            _commandManager = commandManager;
        }

        public bool OnBeforeExecute(ICommand command)
        {
            // 只在执行非移动命令时确认待确认的添加节点命令
            if (!(command is MoveNodeCommand))
            {
                _commandManager.ConfirmLastAddNodePosition();
                ClearDropFlags();
            }

            // 总是允许执行
            return true;
        }

        public void OnAfterExecute(ICommand command)
        {
            // 执行后无需特殊处理
        }

        /// <summary>
        /// 清除拖放标志
        /// </summary>
        private void ClearDropFlags()
        {
            try
            {
                // 通过事件系统通知清除拖放标志
                XNode.SubSystem.EventSystem.EM.Instance.Invoke(XNode.SubSystem.EventSystem.EventType.ClearDropFlags);
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"清除拖放标志失败: {ex.Message}");
            }
        }
    }
}
