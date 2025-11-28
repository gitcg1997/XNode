using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.ImageRecognition
{
    /// <summary>
    /// 区域找图节点
    /// 在指定屏幕区域内查找单个匹配结果
    /// </summary>
    public class RegionFindImageNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_TEMPLATE_PATH = 1;
        private const int PIN_GROUP_REGION_X = 2;
        private const int PIN_GROUP_REGION_Y = 3;
        private const int PIN_GROUP_REGION_WIDTH = 4;
        private const int PIN_GROUP_REGION_HEIGHT = 5;
        private const int PIN_GROUP_THRESHOLD = 6;
        private const int PIN_GROUP_FOUND_X = 7;
        private const int PIN_GROUP_FOUND_Y = 8;
        private const int PIN_GROUP_FOUND_CONFIDENCE = 9;
        private const int PIN_GROUP_ACTION_FOUND = 10;
        private const int PIN_GROUP_ACTION_NOT_FOUND = 11;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 100, g = 150, b = 255 },
                "CPU",
                "区域找图"
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

            PinGroupList.Add(new DataPinGroup(this, "int", "区域X", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域Y", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域宽度", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域高度", "0")
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

            PinGroupList.Add(new DataPinGroup(this, "double", "置信度", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 100
            });

            PinGroupList.Add(new ActionPinGroup(this, "匹配成功"));
            PinGroupList.Add(new ActionPinGroup(this, "未匹配"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_TEMPLATE_PATH);
                UpdateData(PIN_GROUP_REGION_X);
                UpdateData(PIN_GROUP_REGION_Y);
                UpdateData(PIN_GROUP_REGION_WIDTH);
                UpdateData(PIN_GROUP_REGION_HEIGHT);
                UpdateData(PIN_GROUP_THRESHOLD);

                string templatePath = GetData(PIN_GROUP_TEMPLATE_PATH);
                if (!int.TryParse(GetData(PIN_GROUP_REGION_X), out int regionX))
                    regionX = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_Y), out int regionY))
                    regionY = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_WIDTH), out int regionWidth))
                    regionWidth = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_HEIGHT), out int regionHeight))
                    regionHeight = 0;
                if (!double.TryParse(GetData(PIN_GROUP_THRESHOLD), out double threshold))
                    threshold = 0.8;

                if (string.IsNullOrWhiteSpace(templatePath))
                {
                    Console.WriteLine("[RegionFindImageNode] 模板图像路径为空");
                    ExecuteNotFound();
                    return;
                }

                if (!File.Exists(templatePath))
                {
                    Console.WriteLine($"[RegionFindImageNode] 模板图像文件不存在: {templatePath}");
                    ExecuteNotFound();
                    return;
                }

                if (regionWidth <= 0 || regionHeight <= 0)
                {
                    Console.WriteLine("[RegionFindImageNode] 区域宽高必须大于 0");
                    ExecuteNotFound();
                    return;
                }

                Console.WriteLine($"[RegionFindImageNode] 区域[{regionX},{regionY},{regionWidth},{regionHeight}] 查找图像: {Path.GetFileName(templatePath)}");

                // 捕获指定区域
                using var regionBitmap = CaptureRegion(regionX, regionY, regionWidth, regionHeight);
                using var templateBitmap = new Bitmap(templatePath);

                // 执行模板匹配
                var result = TemplateMatch(regionBitmap, templateBitmap, threshold);

                if (result.HasValue)
                {
                    // 转换为屏幕坐标
                    int screenX = regionX + result.Value.X;
                    int screenY = regionY + result.Value.Y;

                    SetData(PIN_GROUP_FOUND_X, screenX.ToString());
                    SetData(PIN_GROUP_FOUND_Y, screenY.ToString());
                    SetData(PIN_GROUP_FOUND_CONFIDENCE, result.Value.Confidence.ToString("F3"));

                    Console.WriteLine($"[RegionFindImageNode] 匹配成功, 位置: ({screenX}, {screenY}), 置信度: {result.Value.Confidence:F3}");
                    ExecuteFound();
                }
                else
                {
                    ResetOutputs();
                    Console.WriteLine("[RegionFindImageNode] 未找到匹配图像");
                    ExecuteNotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegionFindImageNode] 执行区域找图时发生错误: {ex.Message}");
                ResetOutputs();
                ExecuteNotFound();
                throw;
            }
        }

        private Bitmap CaptureRegion(int x, int y, int width, int height)
        {
            var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(x, y, 0, 0, new Size(width, height));
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

        private void ExecuteFound()
        {
            var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_ACTION_FOUND);
            actionGroup.Invoke();
        }

        private void ExecuteNotFound()
        {
            var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_ACTION_NOT_FOUND);
            actionGroup.Invoke();
        }

        private void ResetOutputs()
        {
            SetData(PIN_GROUP_FOUND_X, "0");
            SetData(PIN_GROUP_FOUND_Y, "0");
            SetData(PIN_GROUP_FOUND_CONFIDENCE, "0");
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(RegionFindImageNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "TemplatePath", GetData(PIN_GROUP_TEMPLATE_PATH) },
                { "RegionX", GetData(PIN_GROUP_REGION_X) },
                { "RegionY", GetData(PIN_GROUP_REGION_Y) },
                { "RegionWidth", GetData(PIN_GROUP_REGION_WIDTH) },
                { "RegionHeight", GetData(PIN_GROUP_REGION_HEIGHT) },
                { "Threshold", GetData(PIN_GROUP_THRESHOLD) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("TemplatePath", out string? templatePath))
                SetData(PIN_GROUP_TEMPLATE_PATH, templatePath);
            if (paraDict.TryGetValue("RegionX", out string? regionX))
                SetData(PIN_GROUP_REGION_X, regionX);
            if (paraDict.TryGetValue("RegionY", out string? regionY))
                SetData(PIN_GROUP_REGION_Y, regionY);
            if (paraDict.TryGetValue("RegionWidth", out string? regionWidth))
                SetData(PIN_GROUP_REGION_WIDTH, regionWidth);
            if (paraDict.TryGetValue("RegionHeight", out string? regionHeight))
                SetData(PIN_GROUP_REGION_HEIGHT, regionHeight);
            if (paraDict.TryGetValue("Threshold", out string? threshold))
                SetData(PIN_GROUP_THRESHOLD, threshold);
        }

        protected override NodeBase CloneNode() => new RegionFindImageNode();

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
