using System.Runtime.InteropServices;
using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.Actions
{
    /// <summary>
    /// 鼠标点击节点
    /// 在指定坐标执行鼠标点击操作
    /// </summary>
    public class MouseClickNode : NodeBase
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        #endregion

        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_X = 1;
        private const int PIN_GROUP_Y = 2;
        private const int PIN_GROUP_CLICK_TYPE = 3;
        private const int PIN_GROUP_EXECUTE_OUT = 4;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 100, g = 255, b = 150 },
                "CPU",
                "鼠标点击"
            );

            PinGroupList.Clear();

            // 输入执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            // X坐标参数
            PinGroupList.Add(new DataPinGroup(this, "int", "X坐标", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            // Y坐标参数
            PinGroupList.Add(new DataPinGroup(this, "int", "Y坐标", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            // 点击类型参数 - 使用下拉选择框
            var clickTypeGroup = new ComboBoxPinGroup(this, "string", "点击类型", "click")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            };
            clickTypeGroup.SetOptions(
                new List<string>
                {
                    "click",
                    "double_click",
                    "right_click",
                    "middle_click",
                    "left_down",
                    "left_up",
                    "right_down",
                    "right_up"
                },
                new Dictionary<string, string>
                {
                    { "click", "左键单击" },
                    { "double_click", "左键双击" },
                    { "right_click", "右键单击" },
                    { "middle_click", "中键单击" },
                    { "left_down", "左键按下" },
                    { "left_up", "左键释放" },
                    { "right_down", "右键按下" },
                    { "right_up", "右键释放" }
                }
            );
            PinGroupList.Add(clickTypeGroup);

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
                UpdateData(PIN_GROUP_X);
                UpdateData(PIN_GROUP_Y);
                UpdateData(PIN_GROUP_CLICK_TYPE);

                if (!int.TryParse(GetData(PIN_GROUP_X), out int x))
                    x = 0;
                if (!int.TryParse(GetData(PIN_GROUP_Y), out int y))
                    y = 0;
                
                string clickType = GetData(PIN_GROUP_CLICK_TYPE);
                if (string.IsNullOrWhiteSpace(clickType))
                    clickType = "click";

                // 移动鼠标到指定位置
                SetCursorPos(x, y);
                Thread.Sleep(10);

                // 执行点击
                PerformClick(clickType);

                Console.WriteLine($"[MouseClickNode] 执行鼠标点击: 位置({x}, {y}), 类型: {clickType}");

                // 触发输出执行引脚
                var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_EXECUTE_OUT);
                actionGroup.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MouseClickNode] 执行鼠标点击节点时发生错误: {ex.Message}");
                throw;
            }
        }

        private void PerformClick(string clickType)
        {
            switch (clickType.ToLower())
            {
                case "click":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case "double_click":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    Thread.Sleep(50);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case "right_click":
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
                case "middle_click":
                    mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                    break;
                case "left_down":
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    break;
                case "left_up":
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
                case "right_down":
                    mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                    break;
                case "right_up":
                    mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                    break;
                default:
                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                    Thread.Sleep(10);
                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                    break;
            }
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(MouseClickNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "X", GetData(PIN_GROUP_X) },
                { "Y", GetData(PIN_GROUP_Y) },
                { "ClickType", GetData(PIN_GROUP_CLICK_TYPE) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("X", out string? x))
                SetData(PIN_GROUP_X, x);
            if (paraDict.TryGetValue("Y", out string? y))
                SetData(PIN_GROUP_Y, y);
            if (paraDict.TryGetValue("ClickType", out string? clickType))
                SetData(PIN_GROUP_CLICK_TYPE, clickType);
        }

        protected override NodeBase CloneNode() => new MouseClickNode();

        #endregion
    }
}
