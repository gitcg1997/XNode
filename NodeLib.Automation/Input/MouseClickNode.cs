using System.Runtime.InteropServices;
using XLib.Node;

namespace NodeLib.Automation.Input
{
    /// <summary>
    /// 鼠标点击节点
    /// </summary>
    public class MouseClickNode : NodeBase
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, int dwExtraInfo);

        private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const uint MOUSEEVENTF_LEFTUP = 0x0004;
        private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const uint MOUSEEVENTF_RIGHTUP = 0x0010;
        private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

        #endregion

        public override void Init()
        {
            SetViewProperty(NodeColorSet.Function, "Function", "鼠标点击");

            // 执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "执行鼠标点击操作"));

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

            // 输入引脚：按钮类型
            PinGroupList.Add(new DataPinGroup(this, "string", "按钮", "左键")
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
                string button = GetData(3);

                if (string.IsNullOrEmpty(xStr) || string.IsNullOrEmpty(yStr))
                {
                    throw new Exception("坐标不能为空");
                }

                int x = int.Parse(xStr);
                int y = int.Parse(yStr);

                // 移动鼠标到指定位置
                System.Windows.Forms.Cursor.Position = new System.Drawing.Point(x, y);

                // 短暂延迟，确保鼠标移动完成
                System.Threading.Thread.Sleep(50);

                // 执行点击
                switch (button.ToLower())
                {
                    case "左键":
                    case "left":
                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                        break;

                    case "右键":
                    case "right":
                        mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
                        break;

                    case "中键":
                    case "middle":
                        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 0);
                        System.Threading.Thread.Sleep(50);
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 0);
                        break;

                    default:
                        throw new Exception($"不支持的按钮类型: {button}");
                }

                // 执行下一个节点
                GetPinGroup<ExecutePinGroup>().Execute();
            }
            catch (Exception ex)
            {
                InvokeExecuteError(ex);
            }
        }

        public override string GetTypeString() => nameof(MouseClickNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "X", GetData(1) },
                { "Y", GetData(2) },
                { "Button", GetData(3) }
            };
        }

        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            SetData(1, paraDict["X"]);
            SetData(2, paraDict["Y"]);
            SetData(3, paraDict["Button"]);
        }

        protected override NodeBase CloneNode() => new MouseClickNode();
    }
}
