using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.ImageRecognition
{
    /// <summary>
    /// 等待图像节点
    /// 支持等待图像出现或消失,并处理超时
    /// </summary>
    public class WaitForImageNode : NodeBase
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_CXSCREEN = 0;
        private const int SM_CYSCREEN = 1;

        #endregion

        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_TEMPLATE_PATH = 1;
        private const int PIN_GROUP_WAIT_MODE = 2;
        private const int PIN_GROUP_TIMEOUT = 3;
        private const int PIN_GROUP_CHECK_INTERVAL = 4;
        private const int PIN_GROUP_THRESHOLD = 5;
        private const int PIN_GROUP_FOUND_X = 6;
        private const int PIN_GROUP_FOUND_Y = 7;
        private const int PIN_GROUP_ACTION_SUCCESS = 8;
        private const int PIN_GROUP_ACTION_TIMEOUT = 9;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 100, g = 150, b = 255 },
                "CPU",
                "等待图像"
            );

            PinGroupList.Clear();

            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            PinGroupList.Add(new ImagePathPinGroup(this, "string", "模板路径", "")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 140
            });

            // 等待模式: Appear(出现) / Disappear(消失)
            PinGroupList.Add(new DataPinGroup(this, "string", "等待模式", "Appear")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 120
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "超时时间(ms)", "10000")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "检查间隔(ms)", "500")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "double", "相似度阈值", "0.8")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "X坐标", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "Y坐标", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 100
            });

            PinGroupList.Add(new ActionPinGroup(this, "条件满足"));
            PinGroupList.Add(new ActionPinGroup(this, "超时"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_TEMPLATE_PATH);
                UpdateData(PIN_GROUP_WAIT_MODE);
                UpdateData(PIN_GROUP_TIMEOUT);
                UpdateData(PIN_GROUP_CHECK_INTERVAL);
                UpdateData(PIN_GROUP_THRESHOLD);

                string templatePath = GetData(PIN_GROUP_TEMPLATE_PATH);
                string waitMode = GetData(PIN_GROUP_WAIT_MODE);
                if (!int.TryParse(GetData(PIN_GROUP_TIMEOUT), out int timeout))
                    timeout = 10000;
                if (!int.TryParse(GetData(PIN_GROUP_CHECK_INTERVAL), out int checkInterval))
                    checkInterval = 500;
                if (!double.TryParse(GetData(PIN_GROUP_THRESHOLD), out double threshold))
                    threshold = 0.8;

                if (string.IsNullOrWhiteSpace(waitMode))
                    waitMode = "Appear";

                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    Console.WriteLine("[WaitForImageNode] 模板图像路径为空");
                    ExecuteTimeout();
                    return;
                }

                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"[WaitForImageNode] 模板图像文件不存在: {templatePath}");
                    ExecuteTimeout();
                    return;
                }

                bool waitForDisappear = string.Equals(waitMode, "Disappear", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(waitMode, "消失", StringComparison.OrdinalIgnoreCase);

                using var templateBitmap = new Bitmap(templatePath);

                if (waitForDisappear)
                {
                    Console.WriteLine($"[WaitForImageNode] 等待图像消失: {Path.GetFileName(templatePath)}, 超时: {timeout}ms");
                    bool disappeared = WaitForDisappear(templateBitmap, timeout, checkInterval, threshold);

                    if (disappeared)
                    {
                        ResetOutputs();
                        Console.WriteLine("[WaitForImageNode] 目标图像已消失");
                        ExecuteSuccess();
                    }
                    else
                    {
                        ResetOutputs();
                        Console.WriteLine("[WaitForImageNode] 等待超时,图像仍存在");
                        ExecuteTimeout();
                    }
                }
                else
                {
                    Console.WriteLine($"[WaitForImageNode] 等待图像出现: {Path.GetFileName(templatePath)}, 超时: {timeout}ms");
                    var result = WaitForAppear(templateBitmap, timeout, checkInterval, threshold);

                    if (result.HasValue)
                    {
                        SetData(PIN_GROUP_FOUND_X, result.Value.X.ToString());
                        SetData(PIN_GROUP_FOUND_Y, result.Value.Y.ToString());
                        Console.WriteLine($"[WaitForImageNode] 找到图像, 位置: ({result.Value.X}, {result.Value.Y})");
                        ExecuteSuccess();
                    }
                    else
                    {
                        ResetOutputs();
                        Console.WriteLine("[WaitForImageNode] 等待超时,未找到图像");
                        ExecuteTimeout();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WaitForImageNode] 执行等待图像节点时发生错误: {ex.Message}");
                ResetOutputs();
                ExecuteTimeout();
                throw;
            }
        }

        private MatchResult? WaitForAppear(Bitmap template, int timeout, int interval, double threshold)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                using var screen = CaptureScreen();
                var result = TemplateMatch(screen, template, threshold);
                if (result.HasValue)
                    return result;

                Thread.Sleep(interval);
            }
            return null;
        }

        private bool WaitForDisappear(Bitmap template, int timeout, int interval, double threshold)
        {
            var startTime = DateTime.Now;
            while ((DateTime.Now - startTime).TotalMilliseconds < timeout)
            {
                using var screen = CaptureScreen();
                var result = TemplateMatch(screen, template, threshold);
                if (!result.HasValue)
                    return true;

                Thread.Sleep(interval);
            }
            return false;
        }

        private Bitmap CaptureScreen()
        {
            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(0, 0, 0, 0, new Size(width, height));
            }
            return bitmap;
        }

        private MatchResult? TemplateMatch(Bitmap source, Bitmap template, double threshold)
        {
            int sourceWidth = source.Width;
            int sourceHeight = source.Height;
            int templateWidth = template.Width;
            int templateHeight = template.Height;

            if (templateWidth > sourceWidth || templateHeight > sourceHeight)
                return null;

            double bestMatch = 0;
            int bestX = 0, bestY = 0;
            int step = 2;

            var sourceData = GetBitmapData(source);
            var templateData = GetBitmapData(template);

            for (int y = 0; y <= sourceHeight - templateHeight; y += step)
            {
                for (int x = 0; x <= sourceWidth - templateWidth; x += step)
                {
                    double match = CalculateMatch(sourceData, templateData, sourceWidth, x, y, templateWidth, templateHeight);
                    if (match > bestMatch)
                    {
                        bestMatch = match;
                        bestX = x;
                        bestY = y;
                    }
                }
            }

            if (bestMatch >= threshold)
            {
                return new MatchResult
                {
                    X = bestX + templateWidth / 2,
                    Y = bestY + templateHeight / 2,
                    Confidence = bestMatch
                };
            }

            return null;
        }

        private byte[] GetBitmapData(Bitmap bitmap)
        {
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            var bytes = new byte[data.Stride * bitmap.Height];
            Marshal.Copy(data.Scan0, bytes, 0, bytes.Length);
            bitmap.UnlockBits(data);
            return bytes;
        }

        private double CalculateMatch(byte[] source, byte[] template, int sourceWidth, int offsetX, int offsetY, int templateWidth, int templateHeight)
        {
            int sourceStride = sourceWidth * 3;
            int templateStride = templateWidth * 3;

            long totalDiff = 0;
            int sampleCount = 0;
            int sampleStep = 4;

            for (int y = 0; y < templateHeight; y += sampleStep)
            {
                for (int x = 0; x < templateWidth; x += sampleStep)
                {
                    int sourceIndex = (offsetY + y) * sourceStride + (offsetX + x) * 3;
                    int templateIndex = y * templateStride + x * 3;

                    if (sourceIndex + 2 >= source.Length || templateIndex + 2 >= template.Length)
                        continue;

                    int diffB = Math.Abs(source[sourceIndex] - template[templateIndex]);
                    int diffG = Math.Abs(source[sourceIndex + 1] - template[templateIndex + 1]);
                    int diffR = Math.Abs(source[sourceIndex + 2] - template[templateIndex + 2]);

                    totalDiff += diffB + diffG + diffR;
                    sampleCount++;
                }
            }

            if (sampleCount == 0) return 0;

            double avgDiff = (double)totalDiff / (sampleCount * 3 * 255);
            return 1.0 - avgDiff;
        }

        private void ExecuteSuccess()
        {
            var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_ACTION_SUCCESS);
            actionGroup.Invoke();
        }

        private void ExecuteTimeout()
        {
            var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_ACTION_TIMEOUT);
            actionGroup.Invoke();
        }

        private void ResetOutputs()
        {
            SetData(PIN_GROUP_FOUND_X, "0");
            SetData(PIN_GROUP_FOUND_Y, "0");
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(WaitForImageNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "TemplatePath", GetData(PIN_GROUP_TEMPLATE_PATH) },
                { "WaitMode", GetData(PIN_GROUP_WAIT_MODE) },
                { "Timeout", GetData(PIN_GROUP_TIMEOUT) },
                { "CheckInterval", GetData(PIN_GROUP_CHECK_INTERVAL) },
                { "Threshold", GetData(PIN_GROUP_THRESHOLD) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("TemplatePath", out string? templatePath))
                SetData(PIN_GROUP_TEMPLATE_PATH, templatePath);
            if (paraDict.TryGetValue("WaitMode", out string? waitMode))
                SetData(PIN_GROUP_WAIT_MODE, waitMode);
            if (paraDict.TryGetValue("Timeout", out string? timeout))
                SetData(PIN_GROUP_TIMEOUT, timeout);
            if (paraDict.TryGetValue("CheckInterval", out string? checkInterval))
                SetData(PIN_GROUP_CHECK_INTERVAL, checkInterval);
            if (paraDict.TryGetValue("Threshold", out string? threshold))
                SetData(PIN_GROUP_THRESHOLD, threshold);
        }

        protected override NodeBase CloneNode() => new WaitForImageNode();

        #endregion

        #region 内部类

        private struct MatchResult
        {
            public int X;
            public int Y;
            public double Confidence;
        }

        #endregion
    }
}
