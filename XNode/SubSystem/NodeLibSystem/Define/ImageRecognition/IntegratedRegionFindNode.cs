using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using XLib.Node;
using XNode.SubSystem.NodeLibSystem.Controls;

namespace XNode.SubSystem.NodeLibSystem.Define.ImageRecognition
{
    /// <summary>
    /// 区域查找与选择节点
    /// 集成区域选择和图像查找功能的统一节点
    /// </summary>
    public class IntegratedRegionFindNode : NodeBase
    {
        #region 字段

        private string _templateImagePath = string.Empty;
        private double _threshold = 0.8;
        private Rectangle _searchRegion;
        private RegionSelectorControl? _selectorControl;

        #endregion

        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_TEMPLATE_PATH = 1;
        private const int PIN_GROUP_THRESHOLD = 2;
        private const int PIN_GROUP_REGION_CONTROL = 3;
        private const int PIN_GROUP_FOUND_X = 4;
        private const int PIN_GROUP_FOUND_Y = 5;
        private const int PIN_GROUP_FOUND_WIDTH = 6;
        private const int PIN_GROUP_FOUND_HEIGHT = 7;
        private const int PIN_GROUP_FOUND_CONFIDENCE = 8;
        private const int PIN_GROUP_ACTION_FOUND = 9;
        private const int PIN_GROUP_ACTION_NOT_FOUND = 10;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 80, g = 160, b = 255 },
                "CPU",
                "区域查找与选择"
            );

            PinGroupList.Clear();

            // 执行输入
            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            // 图像模板路径 (使用 ImagePathPinGroup 支持浏览和图像库)
            PinGroupList.Add(new ImagePathPinGroup(this, "string", "模板路径", "")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 140
            });

            // 相似度阈值
            PinGroupList.Add(new DataPinGroup(this, "double", "相似度阈值", "0.8")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            // 区域选择控件
            _selectorControl = new RegionSelectorControl();
            _selectorControl.RegionChanged += OnRegionChanged;
            PinGroupList.Add(new ControlPinGroup(this, _selectorControl));

            // 输出结果
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

            PinGroupList.Add(new DataPinGroup(this, "int", "匹配宽度", "0")
            {
                Writeable = false,
                Readable = true,
                CanInput = false,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "匹配高度", "0")
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

            // 执行分支
            PinGroupList.Add(new ActionPinGroup(this, "匹配成功"));
            PinGroupList.Add(new ActionPinGroup(this, "未匹配"));

            InitPinGroup();
        }

        private void OnRegionChanged(object? sender, RegionSelectedEventArgs e)
        {
            _searchRegion = e.Region;
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                // 更新输入数据
                UpdateData(PIN_GROUP_TEMPLATE_PATH);
                UpdateData(PIN_GROUP_THRESHOLD);

                _templateImagePath = GetData(PIN_GROUP_TEMPLATE_PATH);
                if (!double.TryParse(GetData(PIN_GROUP_THRESHOLD), out _threshold))
                    _threshold = 0.8;

                // 从控件获取区域
                if (_selectorControl != null)
                    _searchRegion = _selectorControl.GetRegion();

                // 验证参数
                if (string.IsNullOrWhiteSpace(_templateImagePath))
                {
                    Console.WriteLine("[IntegratedRegionFindNode] 模板图像路径为空");
                    ExecuteNotFound();
                    return;
                }

                if (!File.Exists(_templateImagePath))
                {
                    Console.WriteLine($"[IntegratedRegionFindNode] 模板图像文件不存在: {_templateImagePath}");
                    ExecuteNotFound();
                    return;
                }

                if (_searchRegion.Width <= 0 || _searchRegion.Height <= 0)
                {
                    Console.WriteLine("[IntegratedRegionFindNode] 搜索区域无效,宽高必须大于0");
                    ExecuteNotFound();
                    return;
                }

                Console.WriteLine($"[IntegratedRegionFindNode] 区域[{_searchRegion.X},{_searchRegion.Y},{_searchRegion.Width},{_searchRegion.Height}] 查找图像: {Path.GetFileName(_templateImagePath)}");

                // 捕获指定区域
                using var regionBitmap = CaptureRegion(_searchRegion.X, _searchRegion.Y, _searchRegion.Width, _searchRegion.Height);
                using var templateBitmap = new Bitmap(_templateImagePath);

                // 执行模板匹配
                var result = TemplateMatch(regionBitmap, templateBitmap, _threshold);

                if (result.HasValue)
                {
                    // 转换为屏幕坐标
                    int screenX = _searchRegion.X + result.Value.X;
                    int screenY = _searchRegion.Y + result.Value.Y;

                    SetData(PIN_GROUP_FOUND_X, screenX.ToString());
                    SetData(PIN_GROUP_FOUND_Y, screenY.ToString());
                    SetData(PIN_GROUP_FOUND_WIDTH, templateBitmap.Width.ToString());
                    SetData(PIN_GROUP_FOUND_HEIGHT, templateBitmap.Height.ToString());
                    SetData(PIN_GROUP_FOUND_CONFIDENCE, result.Value.Confidence.ToString("F3"));

                    Console.WriteLine($"[IntegratedRegionFindNode] 匹配成功, 位置: ({screenX}, {screenY}), 置信度: {result.Value.Confidence:F3}");
                    ExecuteFound();
                }
                else
                {
                    ResetOutputs();
                    Console.WriteLine("[IntegratedRegionFindNode] 未找到匹配图像");
                    ExecuteNotFound();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IntegratedRegionFindNode] 执行区域查找与选择节点时发生错误: {ex.Message}");
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
            SetData(PIN_GROUP_FOUND_WIDTH, "0");
            SetData(PIN_GROUP_FOUND_HEIGHT, "0");
            SetData(PIN_GROUP_FOUND_CONFIDENCE, "0");
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(IntegratedRegionFindNode);

        public override Dictionary<string, string> GetParaDict()
        {
            var region = _selectorControl?.GetRegion() ?? _searchRegion;
            return new Dictionary<string, string>
            {
                { "TemplatePath", GetData(PIN_GROUP_TEMPLATE_PATH) },
                { "Threshold", GetData(PIN_GROUP_THRESHOLD) },
                { "RegionX", region.X.ToString() },
                { "RegionY", region.Y.ToString() },
                { "RegionWidth", region.Width.ToString() },
                { "RegionHeight", region.Height.ToString() }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("TemplatePath", out string? templatePath))
                SetData(PIN_GROUP_TEMPLATE_PATH, templatePath);
            if (paraDict.TryGetValue("Threshold", out string? threshold))
                SetData(PIN_GROUP_THRESHOLD, threshold);

            // 加载区域参数到控件
            int regionX = 0, regionY = 0, regionWidth = 0, regionHeight = 0;
            if (paraDict.TryGetValue("RegionX", out string? x)) int.TryParse(x, out regionX);
            if (paraDict.TryGetValue("RegionY", out string? y)) int.TryParse(y, out regionY);
            if (paraDict.TryGetValue("RegionWidth", out string? w)) int.TryParse(w, out regionWidth);
            if (paraDict.TryGetValue("RegionHeight", out string? h)) int.TryParse(h, out regionHeight);

            _searchRegion = new Rectangle(regionX, regionY, regionWidth, regionHeight);
            _selectorControl?.SetRegionSilently(regionX, regionY, regionWidth, regionHeight);
        }

        protected override NodeBase CloneNode() => new IntegratedRegionFindNode();

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
