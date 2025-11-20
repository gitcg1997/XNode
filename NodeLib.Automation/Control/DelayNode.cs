using XLib.Node;

namespace NodeLib.Automation.Control
{
    /// <summary>
    /// 延迟节点 - 暂停执行指定的毫秒数
    /// </summary>
    public class DelayNode : NodeBase
    {
        public override void Init()
        {
            SetViewProperty(NodeColorSet.Function, "Function", "延迟");

            // 执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "暂停执行指定时间"));

            // 输入引脚：延迟时间(毫秒)
            PinGroupList.Add(new DataPinGroup(this, "int", "延迟(毫秒)", "1000")
            {
                BoxWidth = 120,
                Readable = false,
                Writeable = false
            });

            InitPinGroup();
        }

        protected override void ExecuteNode()
        {
            try
            {
                // 获取延迟时间
                string delayStr = GetData(1);

                if (string.IsNullOrEmpty(delayStr))
                {
                    throw new Exception("延迟时间不能为空");
                }

                int delayMs = int.Parse(delayStr);

                if (delayMs < 0)
                {
                    throw new Exception("延迟时间不能为负数");
                }

                // 执行延迟
                System.Threading.Thread.Sleep(delayMs);

                // 执行下一个节点
                GetPinGroup<ExecutePinGroup>().Execute();
            }
            catch (Exception ex)
            {
                InvokeExecuteError(ex);
            }
        }

        public override string GetTypeString() => nameof(DelayNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "Delay", GetData(1) }
            };
        }

        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            SetData(1, paraDict["Delay"]);
        }

        protected override NodeBase CloneNode() => new DelayNode();
    }
}
