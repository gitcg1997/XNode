using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Control;

namespace XNode.Command
{
    /// <summary>
    /// 删除节点命令
    /// </summary>
    public class DeleteNodeCommand : CommandBase
    {
        private readonly CoreEditer _editer;
        private readonly NodeBase _node;
        private NodeView? _nodeView;

        // 保存所有连接关系用于恢复
        private readonly List<(PinBase source, PinBase target)> _connections = new List<(PinBase, PinBase)>();

        public override string Description => $"删除节点: {_node.Title}";

        public DeleteNodeCommand(CoreEditer editer, NodeBase node)
        {
            _editer = editer;
            _node = node;

            // 保存节点的所有连接关系
            SaveConnections();
        }

        private void SaveConnections()
        {
            _connections.Clear();
            foreach (var pin in _node.GetAllPin())
            {
                // 保存作为源引脚的连接
                foreach (var target in pin.TargetList)
                {
                    _connections.Add((pin, target));
                }
            }
        }

        public override void Execute()
        {
            LogInfo($"执行删除节点: {_node.Title} (ID: {_node.ID})");

            // 查找对应的NodeView
            _nodeView = _editer.GetEditPanel().FindNodeView(_node);

            // 删除NodeView卡片
            if (_nodeView != null)
            {
                _editer.GetEditPanel().DeleteNodeCard(_nodeView);
            }

            // 删除节点实例(会自动断开所有连接)
            _editer.GetEditPanel().DeleteNode(_node);

            // 更新UI
            _editer.GetEditPanel().UpdateUIAfterNodeOperation();
        }

        public override void Undo()
        {
            LogInfo($"撤销删除节点: {_node.Title} (ID: {_node.ID})");

            // 重新加载节点
            _editer.LoadNode(_node);

            // 恢复所有连接关系
            RestoreConnections();

            // 查找新创建的NodeView
            _nodeView = _editer.GetEditPanel().FindNodeView(_node);

            // 更新UI
            _editer.GetEditPanel().UpdateUIAfterNodeOperation();

            // 通知UI更新
            NotifyUIUpdate();
        }

        public override void Redo()
        {
            LogInfo($"重做删除节点: {_node.Title} (ID: {_node.ID})");
            Execute();

            // 通知UI更新
            NotifyUIUpdate();
        }

        private void RestoreConnections()
        {
            foreach (var (source, target) in _connections)
            {
                // 重新建立连接
                if (!source.TargetList.Contains(target))
                {
                    source.AddTarget(target);
                }
                if (!target.SourceList.Contains(source))
                {
                    target.AddSource(source);
                }

                // 添加连接线到UI
                _editer.GetEditPanel().AddConnectLine(source, target);
            }
        }
    }
}