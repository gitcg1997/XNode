using System.Collections.Generic;
using System.Windows;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Control;
using XNode.SubSystem.NodeEditSystem.Panel;

namespace XNode.Command
{
    /// <summary>
    /// 对齐节点命令
    /// </summary>
    public class AlignNodesCommand : ICommand
    {
        public enum AlignType
        {
            Left,    // 左对齐
            Center,  // 居中对齐
            Right,   // 右对齐
            Top,     // 上对齐
            Bottom   // 下对齐
        }

        private readonly List<NodeView> _nodes;
        private readonly AlignType _alignType;
        private readonly Dictionary<NodeView, NodePoint> _oldPositions;
        private readonly Dictionary<NodeView, NodePoint> _newPositions;
        private readonly EditPanel _editPanel;

        public string Description => $"{GetAlignTypeName()}对齐 {_nodes.Count} 个节点";

        public AlignNodesCommand(
            List<NodeView> nodes,
            AlignType alignType,
            Dictionary<NodeView, NodePoint> oldPositions,
            Dictionary<NodeView, NodePoint> newPositions,
            EditPanel editPanel)
        {
            _nodes = nodes;
            _alignType = alignType;
            _oldPositions = oldPositions;
            _newPositions = newPositions;
            _editPanel = editPanel;
        }

        public void Execute()
        {
            foreach (var node in _nodes)
            {
                if (_newPositions.TryGetValue(node, out var newPos))
                {
                    ApplyPosition(node, newPos);
                }
            }
        }

        public void Undo()
        {
            foreach (var node in _nodes)
            {
                if (_oldPositions.TryGetValue(node, out var oldPos))
                {
                    ApplyPosition(node, oldPos);
                }
            }
        }

        public void Redo()
        {
            Execute();
        }

        private void ApplyPosition(NodeView node, NodePoint position)
        {
            // 更新节点实例坐标(世界坐标)
            node.NodeInstance.Point = new NodePoint(position.X, position.Y);
            // 重要: 同步更新 NodeView 的内部坐标,重置偏移量
            node.Point = new Point(position.X, position.Y);

            // 更新 Canvas 位置 - 需要根据世界中心计算
            // 使用 EditPanel 的方法来刷新节点的 Canvas 位置
            _editPanel.RefreshNodeCanvasPosition(node);
        }

        private string GetAlignTypeName()
        {
            return _alignType switch
            {
                AlignType.Left => "左",
                AlignType.Center => "居中",
                AlignType.Right => "右",
                AlignType.Top => "上",
                AlignType.Bottom => "下",
                _ => "未知"
            };
        }
    }
}
