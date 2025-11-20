using System.Drawing;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using XLib.Node;

namespace NodeLib.Automation.Vision
{
    /// <summary>
    /// 图像查找节点 - 使用模板匹配在屏幕上查找图像
    /// </summary>
    public class FindImageNode : NodeBase
    {
        public override void Init()
        {
            SetViewProperty(NodeColorSet.Function, "Function", "查找图像");

            // 执行引脚
            PinGroupList.Add(new ExecutePinGroup(this, "在屏幕上查找指定图像"));

            // 输入引脚：源图像路径(屏幕截图)
            PinGroupList.Add(new DataPinGroup(this, "string", "源图像", "")
            {
                BoxWidth = 250,
                Readable = false,
                Writeable = false
            });

            // 输入引脚：模板图像路径(要查找的图像)
            PinGroupList.Add(new DataPinGroup(this, "string", "模板图像", "")
            {
                BoxWidth = 250,
                Readable = false,
                Writeable = false
            });

            // 输入引脚：匹配阈值 (0.0 - 1.0)
            PinGroupList.Add(new DataPinGroup(this, "double", "阈值", "0.8")
            {
                BoxWidth = 100,
                Readable = false,
                Writeable = false
            });

            // 输出引脚：是否找到
            PinGroupList.Add(new DataPinGroup(this, "bool", "找到", "false")
            {
                BoxWidth = 80,
                Writeable = false
            });

            // 输出引脚：X坐标
            PinGroupList.Add(new DataPinGroup(this, "int", "X坐标", "0")
            {
                BoxWidth = 100,
                Writeable = false
            });

            // 输出引脚：Y坐标
            PinGroupList.Add(new DataPinGroup(this, "int", "Y坐标", "0")
            {
                BoxWidth = 100,
                Writeable = false
            });

            // 输出引脚：匹配度
            PinGroupList.Add(new DataPinGroup(this, "double", "匹配度", "0")
            {
                BoxWidth = 100,
                Writeable = false
            });

            InitPinGroup();
        }

        protected override void ExecuteNode()
        {
            Mat? sourceMat = null;
            Mat? templateMat = null;

            try
            {
                // 获取参数
                string sourcePath = GetData(1);
                string templatePath = GetData(2);
                string thresholdStr = GetData(3);

                if (string.IsNullOrEmpty(sourcePath))
                {
                    throw new Exception("源图像路径不能为空");
                }

                if (string.IsNullOrEmpty(templatePath))
                {
                    throw new Exception("模板图像路径不能为空");
                }

                if (!File.Exists(sourcePath))
                {
                    throw new Exception($"源图像文件不存在: {sourcePath}");
                }

                if (!File.Exists(templatePath))
                {
                    throw new Exception($"模板图像文件不存在: {templatePath}");
                }

                double threshold = string.IsNullOrEmpty(thresholdStr) ? 0.8 : double.Parse(thresholdStr);

                // 加载图像
                using (Bitmap sourceBitmap = new Bitmap(sourcePath))
                using (Bitmap templateBitmap = new Bitmap(templatePath))
                {
                    sourceMat = BitmapConverter.ToMat(sourceBitmap);
                    templateMat = BitmapConverter.ToMat(templateBitmap);

                    // 执行模板匹配
                    using (Mat result = new Mat())
                    {
                        Cv2.MatchTemplate(sourceMat, templateMat, result, TemplateMatchModes.CCoeffNormed);

                        // 获取最佳匹配位置
                        Cv2.MinMaxLoc(result, out double minVal, out double maxVal, out OpenCvSharp.Point minLoc, out OpenCvSharp.Point maxLoc);

                        // 判断是否找到
                        bool found = maxVal >= threshold;

                        // 设置输出
                        SetData(4, found.ToString());

                        if (found)
                        {
                            // 计算中心点坐标
                            int centerX = maxLoc.X + templateMat.Width / 2;
                            int centerY = maxLoc.Y + templateMat.Height / 2;

                            SetData(5, centerX.ToString());
                            SetData(6, centerY.ToString());
                            SetData(7, maxVal.ToString("F4"));
                        }
                        else
                        {
                            SetData(5, "0");
                            SetData(6, "0");
                            SetData(7, maxVal.ToString("F4"));
                        }
                    }
                }

                // 执行下一个节点
                GetPinGroup<ExecutePinGroup>().Execute();
            }
            catch (Exception ex)
            {
                InvokeExecuteError(ex);
            }
            finally
            {
                // 释放资源
                sourceMat?.Dispose();
                templateMat?.Dispose();
            }
        }

        public override string GetTypeString() => nameof(FindImageNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "SourcePath", GetData(1) },
                { "TemplatePath", GetData(2) },
                { "Threshold", GetData(3) }
            };
        }

        public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
        {
            SetData(1, paraDict["SourcePath"]);
            SetData(2, paraDict["TemplatePath"]);
            SetData(3, paraDict["Threshold"]);
        }

        protected override NodeBase CloneNode() => new FindImageNode();
    }
}
