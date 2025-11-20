using XLib.Node;

namespace XNode.Command
{
    /// <summary>
    /// 移动节点命令 - 支持命令合并
    /// </summary>
    public class MoveNodeCommand : CommandBase, IMergeable
    {
        private readonly NodeBase _node;
        private readonly NodePoint _oldPosition;
        private readonly NodePoint _newPosition;

        public override string Description => $"移动节点: {_node.Title}";

        public MoveNodeCommand(NodeBase node, NodePoint oldPosition, NodePoint newPosition)
        {
            _node = node;
            // 创建新实例以避免引用问题
            _oldPosition = new NodePoint(oldPosition.X, oldPosition.Y);
            _newPosition = new NodePoint(newPosition.X, newPosition.Y);
        }

        public override void Execute()
        {
            _node.Point.X = _newPosition.X;
            _node.Point.Y = _newPosition.Y;
            LogInfo($"执行移动节点: {_node.Title}, 位置: ({_newPosition.X}, {_newPosition.Y})");
        }

        public override void Undo()
        {
            _node.Point.X = _oldPosition.X;
            _node.Point.Y = _oldPosition.Y;
            LogInfo($"撤销移动节点: {_node.Title}, 恢复位置: ({_oldPosition.X}, {_oldPosition.Y})");

            // 更新UI
            NotifyUIUpdate();
        }

        public override void Redo()
        {
            _node.Point.X = _newPosition.X;
            _node.Point.Y = _newPosition.Y;
            LogInfo($"重做移动节点: {_node.Title}, 位置: ({_newPosition.X}, {_newPosition.Y})");

            // 更新UI
            NotifyUIUpdate();
        }

        /// <summary>
        /// 判断是否可以与另一个移动命令合并
        /// </summary>
        /// <remarks>
        /// 只有对同一个节点的连续移动操作才能合并
        /// </remarks>
        public bool CanMergeWith(ICommand other)
        {
            return other is MoveNodeCommand move && move._node == _node;
        }

        /// <summary>
        /// 与另一个移动命令合并
        /// </summary>
        /// <remarks>
        /// 合并后保留最初的起始位置和最新的结束位置
        /// 例如: Move(A->B) + Move(B->C) = Move(A->C)
        /// </remarks>
        public ICommand MergeWith(ICommand other)
        {
            var move = (MoveNodeCommand)other;
            // 保留当前命令的起始位置，使用新命令的结束位置
            return new MoveNodeCommand(_node, _oldPosition, move._newPosition);
        }
    }
}