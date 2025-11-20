using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using XLib.Node;

namespace NodeLib.Automation.Vision
{
    /// <summary>
    /// 屏幕截图节点 - 捕获屏幕区域
    /// </summary>
    public class CaptureScreenNode : NodeBase
    {
        public override void Init()
        {
            SetViewProperty(NodeColorSet.Function, "Function", "屏幕截图");

            // 执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "捕获屏幕指定区域"));

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

            // 输入引脚：宽度
            PinGroupList.Add(new DataPinGroup(this, "int", "宽度", "1920")
            {
                BoxWidth = 100,
                Readable = false,
                Writeable = false
            });

            // 输入引脚：高度
            PinGroupList.Add(new DataPinGroup(this, "int", "高度", "1080")
            {
                BoxWidth = 100,
                Readable = false,
                Writeable = false
            });

            // 输出引脚：保存路径
            PinGroupList.Add(new DataPinGroup(this, "string", "保存路径", "")
            {
                BoxWidth = 250,
                Readable = false,
                Writeable = false
            });

            // 输出引脚：截图数据(临时文件路径)
            PinGroupList.Add(new DataPinGroup(this, "string", "截图", "")
            {
                BoxWidth = 250,
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
                string widthStr = GetData(3);
                string heightStr = GetData(4);
                string savePath = GetData(5);

                if (string.IsNullOrEmpty(xStr) || string.IsNullOrEmpty(yStr) ||
                    string.IsNullOrEmpty(widthStr) || string.IsNullOrEmpty(heightStr))
                {
                    throw new Exception("截图区域参数不能为空");
                }

                int x = int.Parse(xStr);
                int y = int.Parse(yStr);
                int width = int.Parse(widthStr);
                int height = int.Parse(heightStr);

                if (width <= 0 || height <= 0)
                {
                    throw new Exception("宽度和高度必须大于0");
                }

                // 创建截图
                using (Bitmap bitmap = new Bitmap(width, height))
                {
                    using (Graphics graphics = Graphics.FromImage(bitmap))
                    {
                        graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
                    }

                    // 确定保存路径
                    string finalPath;
                    if (string.IsNullOrEmpty(savePath))
                    {
                        // 如果没有指定路径，保存到临时目录
                        string tempPath = Path.Combine(Path.GetTempPath(), "XNode_Screenshots");
                        Directory.CreateDirectory(tempPath);
                        finalPath = Path.Combine(tempPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                    }
                    else
                    {
                        finalPath = savePath;
                        // 确保目录存在
                        string? directory = Path.GetDirectoryName(finalPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                    }

                    // 保存截图
                    bitmap.Save(finalPath, ImageFormat.Png);

                    // 输出截图路径
                    SetData(6, finalPath);
                }

                // 执行下一个节点
                GetPinGroup<ExecutePinGroup>().Execute();
            }
            catch (Exception ex)
            {
                InvokeExecuteError(ex);
            }
        }

        public override string GetTypeString() => nameof(CaptureScreenNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "X", GetData(1) },
                { "Y", GetData(2) },
                { "Width", GetData(3) },
                { "Height", GetData(4) },
                { "SavePath", GetData(5) }
            };
        }

        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            SetData(1, paraDict["X"]);
            SetData(2, paraDict["Y"]);
            SetData(3, paraDict["Width"]);
            SetData(4, paraDict["Height"]);
            SetData(5, paraDict["SavePath"]);
        }

        protected override NodeBase CloneNode() => new CaptureScreenNode();
    }
}
