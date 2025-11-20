using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Control;

namespace XNode.Command
{
    /// <summary>
    /// 智能添加节点命令 - 支持拖放后位置动态更新
    /// </summary>
    public class AddNodeCommand : CommandBase
    {
        private readonly CoreEditer _editer;
        private readonly NodeBase _node;
        private NodeView? _nodeView;

        /// <summary>节点的初始位置（拖放时的位置）</summary>
        private readonly NodePoint _initialPosition;

        /// <summary>是否处于待确认状态（可以动态更新位置）</summary>
        private bool _isPending = true;

        public override string Description => $"添加节点: {_node.Title}";

        public AddNodeCommand(CoreEditer editer, NodeBase node)
        {
            _editer = editer;
            _node = node;
            // 记录初始位置
            _initialPosition = new NodePoint(node.Point.X, node.Point.Y);
        }

        /// <summary>
        /// 尝试更新节点位置（仅在待确认状态下有效）
        /// </summary>
        /// <param name="newPosition">新位置</param>
        /// <returns>是否成功更新</returns>
        public bool TryUpdatePosition(NodePoint newPosition)
        {
            if (_isPending)
            {
                _node.Point.X = newPosition.X;
                _node.Point.Y = newPosition.Y;
                LogInfo($"动态更新节点位置: {_node.Title} -> ({newPosition.X}, {newPosition.Y})");
                return true; // 成功更新，跳过创建移动命令
            }
            return false; // 已确认，需要创建移动命令
        }

        /// <summary>
        /// 确认节点位置，结束待确认状态
        /// </summary>
        public void ConfirmPosition()
        {
            if (_isPending)
            {
                _isPending = false;
                LogInfo($"确认节点位置: {_node.Title} 在 ({_node.Point.X}, {_node.Point.Y})");
            }
        }

        /// <summary>
        /// 检查是否处于待确认状态
        /// </summary>
        public bool IsPending => _isPending;

        /// <summary>
        /// 获取节点实例
        /// </summary>
        public NodeBase Node => _node;

        public override void Execute()
        {
            _editer.LoadNode(_node);
            // 查找对应的NodeView
            _nodeView = _editer.GetEditPanel().FindNodeView(_node);

            LogInfo($"执行添加节点: {_node.Title} (ID: {_node.ID})");
        }

        public override void Undo()
        {
            LogInfo($"撤销添加节点: {_node.Title} (ID: {_node.ID})");

            // 确认位置状态（如果还在待确认状态）
            ConfirmPosition();

            // 删除NodeView卡片
            if (_nodeView != null)
            {
                _editer.GetEditPanel().DeleteNodeCard(_nodeView);
            }

            // 删除节点实例
            _editer.GetEditPanel().DeleteNode(_node);

            // 更新UI
            _editer.GetEditPanel().UpdateUIAfterNodeOperation();

            // 清除选中状态，避免撤销后选中框仍然显示
            ClearSelection();

            // 通知UI更新
            NotifyUIUpdate();
        }

        public override void Redo()
        {
            LogInfo($"重做添加节点: {_node.Title} (ID: {_node.ID})");

            // 重做时不再进入待确认状态，直接确认位置
            _isPending = false;

            Execute();
            // 重做后需要更新UI
            _editer.GetEditPanel().UpdateUIAfterNodeOperation();

            // 通知UI更新
            NotifyUIUpdate();
        }
        
        /// <summary>
        /// 清除选中状态
        /// </summary>
        private void ClearSelection()
        {
            try
            {
                var editPanel = _editer.GetEditPanel();
                // 通过InteractionComponent清除选中
                editPanel.InteractionComponent?.ClearSelect();

                // 通过DrawingComponent清除选中框
                editPanel.DrawingComponent?.ClearSelectBox();

                LogInfo("撤销添加节点后已清除选中状态和选中框");
            }
            catch (Exception ex)
            {
                LogError($"清除选中状态失败: {ex.Message}");
            }
        }
    }
}