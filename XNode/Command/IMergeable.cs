namespace XNode.Command
{
    /// <summary>
    /// 可合并命令接口 - 支持将连续的相似命令合并为一个
    /// </summary>
    /// <remarks>
    /// 用于优化撤销/重做历史，例如：
    /// - 连续的移动操作可以合并为一次移动
    /// - 连续的文本编辑可以合并为一次编辑
    /// </remarks>
    public interface IMergeable
    {
        /// <summary>
        /// 判断是否可以与另一个命令合并
        /// </summary>
        /// <param name="other">要合并的命令</param>
        /// <returns>true 表示可以合并，false 表示不可合并</returns>
        bool CanMergeWith(ICommand other);

        /// <summary>
        /// 与另一个命令合并，返回合并后的新命令
        /// </summary>
        /// <param name="other">要合并的命令</param>
        /// <returns>合并后的新命令实例</returns>
        /// <remarks>
        /// 调用此方法前应先调用 CanMergeWith 确认可以合并
        /// </remarks>
        ICommand MergeWith(ICommand other);
    }
}
