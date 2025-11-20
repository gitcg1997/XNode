using System.Collections.Generic;
using XLib.Node;

namespace XLib.Base.Command
{
    /// <summary>
    /// 删除节点命令
    /// </summary>
    public class DeleteNodeCommand : ICommand
    {
        private readonly CoreEditer _editer;
        private readonly NodeBase _node;
        private readonly List<NodeBase> _nodeList;
        private bool _isNodeLoaded = false;

        public string Description => "删除节点";

        public DeleteNodeCommand(CoreEditer editer, NodeBase node)
        {
            _editer = editer;
            _node = node;
            _nodeList = editer.NodeList;
            // 检查节点是否已加载
            _isNodeLoaded = _nodeList.Contains(_node);
        }

        public void Execute()
        {
            _editer.Panel_NodeEditer.DeleteNode(_node);
        }

        public void Undo()
        {
            // 重新加载节点
            _editer.LoadNode(_node);
        }

        public void Redo()
        {
            Execute();
        }
    }
}