using System.Windows;
using System.Windows.Controls;
using XNode.Command;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.ProjectSystem;

namespace XNode
{
    public partial class CoreEditer : UserControl
    {
        #region 属性

        /// <summary>节点列表</summary>
        public List<NodeBase> NodeList => Panel_NodeEditer.NodeList;

        /// <summary>命令管理器</summary>
        public CommandManager CommandManager { get; } = CommandManager.Instance;

        #endregion

        #region 构造方法

        public CoreEditer()
        {
            InitializeComponent();
            Loaded += CoreEditer_Loaded;
        }

        #endregion

        #region 公开方法

        /// <summary>
        /// 重置编辑器
        /// </summary>
        public void ResetEditer() => Panel_NodeEditer.Reset();

        /// <summary>
        /// 加载节点
        /// </summary>
        public void LoadNode(NodeBase node) => Panel_NodeEditer.LoadNode(node);

        /// <summary>
        /// 查找引脚
        /// </summary>
        public PinBase? FindPin(PinPath path) => Panel_NodeEditer.FindPin(path);

        /// <summary>
        /// 执行添加节点命令
        /// </summary>
        public void ExecuteAddNodeCommand(NodeBase node)
        {
            ICommand command = new AddNodeCommand(this, node);
            CommandManager.ExecuteCommand(command);
            MainWindow.LogManager.LogInfo($"添加节点命令已执行, CanUndo: {CommandManager.CanUndo}");
        }

        /// <summary>
        /// 执行删除节点命令
        /// </summary>
        public void ExecuteDeleteNodeCommand(NodeBase node)
        {
            ICommand command = new DeleteNodeCommand(this, node);
            CommandManager.ExecuteCommand(command);
        }

        /// <summary>
        /// 执行移动节点命令
        /// </summary>
        public void ExecuteMoveNodeCommand(NodeBase node, NodePoint oldPosition, NodePoint newPosition)
        {
            ICommand command = new MoveNodeCommand(node, oldPosition, newPosition);
            CommandManager.ExecuteCommand(command);
            MainWindow.LogManager.LogInfo($"移动节点命令已执行, 从 ({oldPosition.X},{oldPosition.Y}) 到 ({newPosition.X},{newPosition.Y})");
        }

        /// <summary>
        /// 执行连接引脚命令
        /// </summary>
        public void ExecuteConnectPinCommand(PinBase sourcePin, PinBase targetPin)
        {
            ICommand command = new ConnectPinCommand(this, sourcePin, targetPin);
            CommandManager.ExecuteCommand(command);
        }

        /// <summary>
        /// 执行断开引脚命令
        /// </summary>
        public void ExecuteDisconnectPinCommand(PinBase sourcePin, PinBase targetPin)
        {
            ICommand command = new DisconnectPinCommand(this, sourcePin, targetPin);
            CommandManager.ExecuteCommand(command);
        }

        /// <summary>
        /// 获取编辑面板实例
        /// </summary>
        public SubSystem.NodeEditSystem.Panel.EditPanel GetEditPanel()
        {
            return Panel_NodeEditer;
        }

        #endregion

        #region 控件事件

        private void CoreEditer_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            ProjectManager.Instance.NewProject();
            ProjectManager.Instance.Saved = true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 初始化核心编辑器
        /// </summary>
        private void Init()
        {
            // 初始化编辑器面板
            Panel_NodeEditer.Init();
            // 初始化节点库面板
            Panel_NodeLib.Init();
            
            // 注意：执行器事件连接已在MainWindow中处理，这里不再需要重复连接
            // 执行器实例在MainWindow中创建和管理
        }

        #endregion
    }
}