using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Controls
{
    /// <summary>
    /// 条件判断节点
    /// 根据条件值分支执行不同的路径
    /// </summary>
    public class ConditionNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_CONDITION = 1;
        private const int PIN_GROUP_TRUE_BRANCH = 2;
        private const int PIN_GROUP_FALSE_BRANCH = 3;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 255, g = 150, b = 100 },
                "CPU",
                "条件判断"
            );

            PinGroupList.Clear();

            // 输入执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            // 条件参数
            PinGroupList.Add(new DataPinGroup(this, "bool", "条件", "False")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            // True分支执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "True"));

            // False分支执行引脚
            PinGroupList.Add(new ActionPinGroup(this, "False"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_CONDITION);

                if (!bool.TryParse(GetData(PIN_GROUP_CONDITION), out bool condition))
                    condition = false;

                Console.WriteLine($"[ConditionNode] 条件判断: {condition}");

                if (condition)
                {
                    Console.WriteLine("[ConditionNode] 执行 True 分支");
                    var trueBranchGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_TRUE_BRANCH);
                    trueBranchGroup.Invoke();
                }
                else
                {
                    Console.WriteLine("[ConditionNode] 执行 False 分支");
                    var falseBranchGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_FALSE_BRANCH);
                    falseBranchGroup.Invoke();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ConditionNode] 执行条件判断节点时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(ConditionNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "Condition", GetData(PIN_GROUP_CONDITION) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("Condition", out string? condition))
                SetData(PIN_GROUP_CONDITION, condition);
        }

        protected override NodeBase CloneNode() => new ConditionNode();

        #endregion
    }
}
