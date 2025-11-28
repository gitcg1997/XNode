using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Actions
{
    /// <summary>
    /// 延迟节点
    /// 暂停执行指定的时间（毫秒）
    /// </summary>
    public class DelayNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_DELAY_TIME = 1;
        private const int PIN_GROUP_EXECUTE_OUT = 2;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 255, g = 200, b = 100 },
                "CPU",
                "延迟"
            );

            PinGroupList.Clear();

            // 输入执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            // 延迟时间参数（毫秒）
            PinGroupList.Add(new DataPinGroup(this, "int", "延迟时间(ms)", "1000")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            // 输出执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "Exit"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_DELAY_TIME);

                if (!int.TryParse(GetData(PIN_GROUP_DELAY_TIME), out int delayMilliseconds))
                    delayMilliseconds = 1000;

                if (delayMilliseconds < 0)
                    delayMilliseconds = 0;

                Console.WriteLine($"[DelayNode] 延迟 {delayMilliseconds} 毫秒");

                if (delayMilliseconds > 0)
                    Thread.Sleep(delayMilliseconds);

                var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_EXECUTE_OUT);
                actionGroup.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DelayNode] 执行延迟节点时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(DelayNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "DelayTime", GetData(PIN_GROUP_DELAY_TIME) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("DelayTime", out string? delayTime))
                SetData(PIN_GROUP_DELAY_TIME, delayTime);
        }

        protected override NodeBase CloneNode() => new DelayNode();

        #endregion
    }
}
