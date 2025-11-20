using XLib.Node;

namespace NodeLib.Automation.Input
{
    /// <summary>
    /// 鼠标移动节点
    /// </summary>
    public class MouseMoveNode : NodeBase
    {
        public override void Init()
        {
            SetViewProperty(NodeColorSet.Function, "Function", "鼠标移动");

            // 执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "移动鼠标到指定位置"));

            // 输入引脚：X坐标
            PinGroupList.Add(new DataPinGroup(this, "int", "X坐标", "0")
            {
                BoxWidth = 100,
                Readable = false,
                Writeable = false
            });

            // 输入引脚：Y坐标
            PinGroupList.Add(new DataPinGroup(this, "int", "Y坐标", "0")
            {
                BoxWidth = 100,
                Readable = false,
                Writeable = false
            });

            InitPinGroup();
        }

        protected override void ExecuteNode()
        {
            try
            {
                // 获取参数
                string xStr = GetData(1);
                string yStr = GetData(2);

                if (string.IsNullOrEmpty(xStr) || string.IsNullOrEmpty(yStr))
                {
                    throw new Exception("坐标不能为空");
                }

                int x = int.Parse(xStr);
                int y = int.Parse(yStr);

                // 移动鼠标到指定位置
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);

                // 执行下一个节点
                GetPinGroup<ExecutePinGroup>().Execute();
            }
            catch (Exception ex)
            {
                InvokeExecuteError(ex);
            }
        }

        public override string GetTypeString() => nameof(MouseMoveNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "X", GetData(1) },
                { "Y", GetData(2) }
            };
        }

        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            SetData(1, paraDict["X"]);
            SetData(2, paraDict["Y"]);
        }

        protected override NodeBase CloneNode() => new MouseMoveNode();
    }
}
