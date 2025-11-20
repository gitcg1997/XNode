namespace XNode.Command
{
    /// <summary>
    /// 命令拦截器接口 - 允许在命令执行前后插入自定义逻辑
    /// </summary>
    /// <remarks>
    /// 使用拦截器模式解耦特殊业务逻辑，保持 CommandManager 的单一职责
    /// </remarks>
    public interface ICommandInterceptor
    {
        /// <summary>
        /// 命令执行前的拦截逻辑
        /// </summary>
        /// <param name="command">即将执行的命令</param>
        /// <returns>true 表示允许执行，false 表示阻止执行</returns>
        bool OnBeforeExecute(ICommand command);

        /// <summary>
        /// 命令执行后的拦截逻辑
        /// </summary>
        /// <param name="command">已执行的命令</param>
        void OnAfterExecute(ICommand command);
    }
}
