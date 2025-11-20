using System.Windows.Controls;
using XLib.Base;
using XLib.Base.UIComponent;
using XLib.Base.VirtualDisk;
using XLib.Node;
using XNode.SubSystem.EventSystem;
using XNode.SubSystem.ExecutionSystem;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.NodeEditSystem.Panel.Component;

namespace XNode.SubSystem.NodeEditSystem.Panel
{
    public partial class EditPanel : UserControl, IDropable
    {
        #region 属性

        /// <summary>节点列表</summary>
        public List<NodeBase> NodeList => _nodeComponent.NodeList;

        /// <summary>操作区域</summary>
        public Grid OperateAreaGrid => (Grid)FindName("OperateArea");

        /// <summary>基础图层</summary>
        public Grid LayerBaseGrid => (Grid)FindName("Layer_Base");

        /// <summary>框图层</summary>
        public Grid LayerBoxGrid => (Grid)FindName("Layer_Box");

        /// <summary>临时图层</summary>
        public Grid LayerTempGrid => (Grid)FindName("Layer_Temp");

        /// <summary>信息图层</summary>
        public Grid LayerInfoGrid => (Grid)FindName("LayerBox_Info");

        /// <summary>工具栏图层</summary>
        public Canvas LayerToolBarCanvas => (Canvas)FindName("LayerBox_ToolBar");

        #endregion

        #region 构造方法

        public EditPanel() => InitializeComponent();

        #endregion

        #region 生命周期

        public void Init()
        {
            // 添加核心组件
            _editerComponent = _componentBox.AddComponent<EditerComponent>(this, "编辑器组件");
            // 添加功能组件
            // 添加功能组件
            _drawingComponent = _componentBox.AddComponent<DrawingComponent>(this, "绘图组件");
            _nodeComponent = _componentBox.AddComponent<NodeComponent>(this, "节点组件");
            _cardComponent = _componentBox.AddComponent<CardComponent>(this, "卡片组件");
            _interactionComponent = _componentBox.AddComponent<InteractionComponent>(this, "交互组件");
            _executionHighlightComponent = _componentBox.AddComponent<ExecutionHighlightComponent>(this, "执行高亮组件");
            _componentBox.RegisterCoreComponent(_editerComponent);
            // 注册功能组件
            _editerComponent.AddComponent(_drawingComponent);
            _editerComponent.AddComponent(_nodeComponent);
            _editerComponent.AddComponent(_cardComponent);
            _editerComponent.AddComponent(_interactionComponent);
            _editerComponent.AddComponent(_executionHighlightComponent);
            // 初始化组件
            _componentBox.Init();
            // 启用编辑
            _editerComponent.ReqEnable();
            // 监听系统事件
            EM.Instance.Add(EventType.Project_Loaded, Project_Loaded);
        }

        #endregion

        #region IDropable 方法

        public void OnDrag(List<ITreeItem> fileList) { }

        public void OnDrop(List<ITreeItem> fileList)
        {
            _interactionComponent.HandleDrop(fileList);
        }

        public bool CanDrop(List<ITreeItem> fileList) => fileList[0] is File file && file.Extension == "nt";

        #endregion

        #region 公开方法

        /// <summary>
        /// 重置
        /// </summary>
        public void Reset() => _componentBox.Reset();

        /// <summary>
        /// 加载节点
        /// </summary>
        public void LoadNode(NodeBase node) => _nodeComponent.LoadNode(node);

        /// <summary>
        /// 删除节点
        /// </summary>
        public void DeleteNode(NodeBase node) => _nodeComponent.DeleteNode(node);

        /// <summary>
        /// 查找NodeView
        /// </summary>
        public Control.NodeView? FindNodeView(NodeBase node)
        {
            foreach (var card in _cardComponent.AllCard)
            {
                if (card.NodeInstance == node)
                    return card;
            }
            return null;
        }

        /// <summary>
        /// 删除NodeView卡片
        /// </summary>
        public void DeleteNodeCard(Control.NodeView nodeView) => _cardComponent.DeleteNodeCard(nodeView);

        /// <summary>
        /// 添加连接线
        /// </summary>
        public void AddConnectLine(PinBase source, PinBase target) => _drawingComponent.AddConnectLine(source, target);

        /// <summary>
        /// 移除连接线
        /// </summary>
        public void RemoveConnectLine(PinBase source, PinBase target) => _drawingComponent.RemoveConnectLine(source, target);

        /// <summary>
        /// 更新所有引脚图标
        /// </summary>
        public void UpdateAllPinIcon() => _interactionComponent.UpdateAllPinIcon();

        /// <summary>
        /// 在节点操作后更新UI
        /// </summary>
        public void UpdateUIAfterNodeOperation()
        {
            // 更新连接线
            _drawingComponent.UpdateConnectLine();
            // 更新选中框
            _drawingComponent.UpdateSelectedBox();
            // 更新引脚图标
            _interactionComponent.UpdateAllPinIcon();
        }

        /// <summary>
        /// 查找引脚
        /// </summary>
        public PinBase? FindPin(PinPath path)
        {
            foreach (var node in _nodeComponent.NodeList)
                if (node.ID == path.NodeID) return node.FindPin(path.NodeVersion, path.GroupIndex, path.PinIndex);
            return null;
        }

        /// <summary>
        /// 高亮节点
        /// </summary>
        public void HighlightNode(NodeBase node) => _executionHighlightComponent.HighlightNode(node);

        /// <summary>
        /// 清除高亮
        /// </summary>
        public void ClearHighlight() => _executionHighlightComponent.ClearHighlight();

        /// <summary>
        /// 连接执行器事件
        /// </summary>
        public void ConnectExecutorEvents(NodeGraphExecutor executor)
        {
            executor.NodeExecutionStarted += (node) => HighlightNode(node);
            executor.NodeExecutionCompleted += (node) => ClearHighlight();
            executor.ExecutionCompleted += () => ClearHighlight();
            executor.ExecutionCancelled += () => ClearHighlight();
            executor.ExecutionError += (ex) => ClearHighlight();
        }

        /// <summary>
        /// 将节点卡片置顶
        /// </summary>
        public void SetNodeCardTop(Control.NodeView nodeView) => _cardComponent.SetTop(nodeView);

        #endregion

        #region 系统事件

        private void Project_Loaded()
        {
            // 更新引脚图标
            _interactionComponent.UpdateAllPinIcon();
            // 生成连接线
            _nodeComponent.GenerateConnectLine();
        }

        #endregion

        #region 字段

        /// <summary>组件箱</summary>
        private readonly ComponentBox<EditPanel> _componentBox = new ComponentBox<EditPanel>();

        /// <summary>编辑器组件</summary>
        private EditerComponent _editerComponent;

        /// <summary>绘图组件</summary>
        private DrawingComponent _drawingComponent;
        /// <summary>节点组件</summary>
        private NodeComponent _nodeComponent;
        /// <summary>卡片组件</summary>
        private CardComponent _cardComponent;
        /// <summary>交互组件</summary>
        private InteractionComponent _interactionComponent;
        /// <summary>执行高亮组件</summary>
        private ExecutionHighlightComponent _executionHighlightComponent;

        /// <summary>交互组件（公共访问）</summary>
        public InteractionComponent InteractionComponent => _interactionComponent;
        /// <summary>绘图组件（公共访问）</summary>
        public DrawingComponent DrawingComponent => _drawingComponent;

        #endregion
    }
}
