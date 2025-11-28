using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Basics
{
    /// <summary>
    /// 结束节点
    /// 节点图的出口点，标记执行流程的结束
    /// </summary>
    public class EndNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE = 0;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化节点
        /// </summary>
        public override void Init()
        {
            // 设置节点视图属性 - 红色（结束）
            SetViewProperty(
                new NodeColor { r = 255, g = 50, b = 50 },
                "CPU",
                "结束"
            );

            // 清空引脚组列表
            PinGroupList.Clear();

            // 输入执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "结束执行"));

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
                Console.WriteLine("[EndNode] 节点图执行完成");

                // 结束节点不需要继续传递执行流
                // 执行流在此终止
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EndNode] 执行结束节点时发生错误: {ex.Message}");
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
            return nameof(EndNode);
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
        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            // 结束节点没有参数需要加载
        }

        /// <summary>
        /// 克隆节点
        /// </summary>
        protected override NodeBase CloneNode()
        {
            return new EndNode();
        }

        #endregion
    }
}
