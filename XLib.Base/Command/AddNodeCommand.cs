using System.Collections.Generic;
using XLib.Node;

namespace XLib.Base.Command
{
    /// <summary>
    /// 添加节点命令
    /// </summary>
    public class AddNodeCommand : ICommand
    {
        private readonly CoreEditer _editer;
        private readonly NodeBase _node;

        public string Description => "添加节点";

        public AddNodeCommand(CoreEditer editer, NodeBase node)
        {
            _editer = editer;
            _node = node;
        }

        public void Execute()
        {
            _editer.LoadNode(_node);
        }

        public void Undo()
        {
            // 通过公开的方法删除节点
            _editer.Panel_NodeEditer.DeleteNode(_node);
        }

        public void Redo()
        {
            Execute();
        }
    }
}