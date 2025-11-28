using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Controls
{
    /// <summary>
    /// 循环节点
    /// 重复执行子流程指定次数
    /// </summary>
    public class LoopNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_LOOP_COUNT = 1;
        private const int PIN_GROUP_CURRENT_INDEX = 2;
        private const int PIN_GROUP_LOOP_BODY = 3;
        private const int PIN_GROUP_COMPLETED = 4;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 200, g = 100, b = 255 },
                "CPU",
                "循环"
            );

            PinGroupList.Clear();

            // 输入执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            // 循环次数参数
            PinGroupList.Add(new DataPinGroup(this, "int", "循环次数", "3")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            // 当前索引输出（从0开始）
            PinGroupList.Add(new DataPinGroup(this, "int", "当前索引", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 80
            });

            // 循环体执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "LoopBody"));

            // 循环完成执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "Completed"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_LOOP_COUNT);

                if (!int.TryParse(GetData(PIN_GROUP_LOOP_COUNT), out int loopCount))
                    loopCount = 3;

                if (loopCount < 0)
                    loopCount = 0;

                Console.WriteLine($"[LoopNode] 开始循环，共 {loopCount} 次");

                for (int currentIndex = 0; currentIndex < loopCount; currentIndex++)
                {
                    // 更新当前索引输出
                    SetData(PIN_GROUP_CURRENT_INDEX, currentIndex.ToString());

                    Console.WriteLine($"[LoopNode] 执行循环第 {currentIndex + 1}/{loopCount} 次");

                    // 触发循环体执行引脚
                    var loopBodyGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_LOOP_BODY);
                    loopBodyGroup.Invoke();
                }

                Console.WriteLine("[LoopNode] 循环完成");

                // 触发完成执行引脚
                var completedGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_COMPLETED);
                completedGroup.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LoopNode] 执行循环节点时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(LoopNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "LoopCount", GetData(PIN_GROUP_LOOP_COUNT) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("LoopCount", out string? loopCount))
                SetData(PIN_GROUP_LOOP_COUNT, loopCount);
        }

        protected override NodeBase CloneNode() => new LoopNode();

        #endregion
    }
}
