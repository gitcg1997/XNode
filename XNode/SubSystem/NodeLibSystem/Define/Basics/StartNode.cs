using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Basics
{
    /// <summary>
    /// 起始节点
    /// 节点图的入口点，每个节点图应该有且仅有一个起始节点
    /// </summary>
    public class StartNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_START = 0;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化节点
        /// </summary>
        public override void Init()
        {
            // 设置节点视图属性 - 绿色（起始）
            SetViewProperty(
                new NodeColor { r = 50, g = 255, b = 50 },
                "CPU",
                "开始"
            );

            // 清空引脚组列表
            PinGroupList.Clear();

            // 输出执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "开始"));

            // 初始化引脚组
            InitPinGroup();
        }

        #endregion

        #region 节点执行

        /// <summary>
        /// 执行节点逻辑
        /// </summary>
        protected override void ExecuteNode()
        {
            try
            {
                Console.WriteLine("[StartNode] 节点图开始执行");

                // 触发输出执行引脚
                var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_START);
                actionGroup.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[StartNode] 执行起始节点时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 序列化

        /// <summary>
        /// 获取类型字符串
        /// </summary>
        public override string GetTypeString()
        {
            return nameof(StartNode);
        }

        /// <summary>
        /// 获取参数表
        /// </summary>
        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>();
        }

        /// <summary>
        /// 加载参数表
        /// </summary>
        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            // 起始节点没有参数需要加载
        }

        /// <summary>
        /// 克隆节点
        /// </summary>
        protected override NodeBase CloneNode()
        {
            return new StartNode();
        }

        #endregion
    }
}
