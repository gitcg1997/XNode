using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using XLib.Animate;
using XLib.Base.UIComponent;
using XLib.Math.Easing;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Control;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.NodeEditSystem.Panel.Layer;

namespace XNode.SubSystem.NodeEditSystem.Panel.Component
{
    /// <summary>
    /// 执行高亮组件：管理节点执行过程中的高亮效果
    /// </summary>
    public class ExecutionHighlightComponent : Component<EditPanel>
    {
        #region 生命周期

        protected override void Init()
        {
            EnableLayer();
        }

        protected override void Reset()
        {
            ClearHighlight();
            _highlightedNodes.Clear();
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 高亮节点
        /// </summary>
        public void HighlightNode(NodeBase node)
        {
            Console.WriteLine($"[ExecutionHighlightComponent] HighlightNode被调用: {node?.Title} (ID: {node?.ID})");
            
            if (node == null)
            {
                Console.WriteLine("[ExecutionHighlightComponent] 节点为null，返回");
                return;
            }

            // 获取节点视图
            NodeView? nodeView = GetComponent<CardComponent>().AllCard.Find(card => card.NodeInstance.ID == node.ID);
            if (nodeView == null)
            {
                Console.WriteLine($"[ExecutionHighlightComponent] 找不到节点视图，节点ID: {node.ID}");
                return;
            }

            Console.WriteLine($"[ExecutionHighlightComponent] 找到节点视图，位置: ({Canvas.GetLeft(nodeView)},{Canvas.GetTop(nodeView)}), 大小: {nodeView.ActualWidth}x{nodeView.ActualHeight}");

            // 清除之前的高亮
            ClearHighlight();

            // 创建高亮框 - 覆盖整个节点区域
            TargetBox highlightBox = new TargetBox
            {
                ScreenPoint = new Point(System.Windows.Controls.Canvas.GetLeft(nodeView) - 5, System.Windows.Controls.Canvas.GetTop(nodeView) - 5),
                Width = nodeView.ActualWidth + 10,
                Height = nodeView.ActualHeight + 10,
                BoxOffset = 0
            };

            // 设置高亮框
            _executionHighlightLayer.HighlightBox = highlightBox;
            _highlightedNodes.Add(node.ID);

            Console.WriteLine($"[ExecutionHighlightComponent] 高亮框已设置，位置: ({highlightBox.ScreenPoint.X},{highlightBox.ScreenPoint.Y}), 大小: {highlightBox.Width}x{highlightBox.Height}");

            // 播放现代高亮动画序列
            PlayHighlightAnimation();
        }

        /// <summary>
        /// 播放高亮动画序列
        /// </summary>
        private void PlayHighlightAnimation()
        {
            // 初始状态：完全透明
            _executionHighlightLayer.HighlightOpacity = 0.0;
            _executionHighlightLayer.GlowIntensity = 0.0;
            _executionHighlightLayer.PulseIntensity = 0.0;

            // 阶段1：淡入效果 (200ms)
            _executionHighlightLayer.Motion("HighlightOpacity", 0, 0.7, 200, EasingType.QuadraticEase, EasingMode.EaseOut);
            
            // 阶段2：发光效果增强 (300ms)
            _executionHighlightLayer.Motion("GlowIntensity", 0, 1.0, 300, EasingType.QuadraticEase, EasingMode.EaseOut);
            
            // 阶段3：脉冲动画 (1000ms循环)
            _executionHighlightLayer.Motion("PulseIntensity", 0, 1.0, 500, EasingType.SineEase, EasingMode.EaseInOut);
            _executionHighlightLayer.Motion("PulseIntensity", 1.0, 0.0, 500, EasingType.SineEase, EasingMode.EaseInOut);
            
            _executionHighlightLayer.Update();
        }

        /// <summary>
        /// 清除高亮
        /// </summary>
        public void ClearHighlight()
        {
            Console.WriteLine("[ExecutionHighlightComponent] ClearHighlight被调用");
            _executionHighlightLayer.HighlightBox = null;
            _executionHighlightLayer.Clear();
            _highlightedNodes.Clear();
        }

        /// <summary>
        /// 检查节点是否被高亮
        /// </summary>
        public bool IsNodeHighlighted(int nodeId) => _highlightedNodes.Contains(nodeId);

        #endregion

        #region 私有方法

        /// <summary>
        /// 启用图层
        /// </summary>
        private void EnableLayer()
        {
            Console.WriteLine("[ExecutionHighlightComponent] 开始启用高亮图层");
            
            // 创建执行高亮图层
            _executionHighlightLayer = new ExecutionHighlightLayer();
            Console.WriteLine($"[ExecutionHighlightComponent] 高亮图层已创建: {_executionHighlightLayer != null}");
            
            // 通过DrawingComponent添加到图层
            var drawingComponent = GetComponent<DrawingComponent>();
            Console.WriteLine($"[ExecutionHighlightComponent] DrawingComponent获取成功: {drawingComponent != null}");
            
            drawingComponent.AddCustomLayer(_executionHighlightLayer);
            Console.WriteLine("[ExecutionHighlightComponent] 高亮图层已添加到DrawingComponent");
            
            // 更新图层尺寸
            UpdateLayerSize();
            
            // 确保图层在最上层
            _executionHighlightLayer.SetValue(Canvas.ZIndexProperty, 1000);
            
            // 强制初始化图层
            _executionHighlightLayer.Update();
            
            Console.WriteLine("[ExecutionHighlightComponent] 高亮图层已启用并初始化");
        }

        /// <summary>
        /// 更新图层尺寸
        /// </summary>
        private void UpdateLayerSize()
        {
            var drawingComponent = GetComponent<DrawingComponent>();
            double width = drawingComponent.GetLayerWidth();
            double height = drawingComponent.GetLayerHeight();

            _executionHighlightLayer.Width = width;
            _executionHighlightLayer.Height = height;
            
            Console.WriteLine($"[ExecutionHighlightComponent] 图层尺寸已更新: {width}x{height}");
        }

        #endregion

        #region 字段

        /// <summary>执行高亮图层</summary>
        private ExecutionHighlightLayer? _executionHighlightLayer;

        /// <summary>当前高亮的节点ID列表</summary>
        private readonly HashSet<int> _highlightedNodes = new HashSet<int>();

        #endregion
    }
}