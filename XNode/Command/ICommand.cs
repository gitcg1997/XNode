namespace XNode.Command
{
    /// <summary>
    /// 命令接口，用于实现撤销/重做功能
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 执行命令
        /// </summary>
        void Execute();

        /// <summary>
        /// 撤销命令
        /// </summary>
        void Undo();

        /// <summary>
        /// 重做命令
        /// </summary>
        void Redo();

        /// <summary>
        /// 命令描述
        /// </summary>
        string Description { get; }
    }
}