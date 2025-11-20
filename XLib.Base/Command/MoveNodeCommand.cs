using XLib.Node;

namespace XLib.Base.Command
{
    /// <summary>
    /// 移动节点命令
    /// </summary>
    public class MoveNodeCommand : ICommand
    {
        private readonly NodeBase _node;
        private readonly NodePoint _oldPosition;
        private readonly NodePoint _newPosition;

        public string Description => "移动节点";

        public MoveNodeCommand(NodeBase node, NodePoint oldPosition, NodePoint newPosition)
        {
            _node = node;
            _oldPosition = oldPosition;
            _newPosition = newPosition;
        }

        public void Execute()
        {
            _node.Point = _newPosition;
        }

        public void Undo()
        {
            _node.Point = _oldPosition;
        }

        public void Redo()
        {
            Execute();
        }
    }
}