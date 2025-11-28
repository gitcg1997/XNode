using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using XNode.Windows.ImageEditor.Models;

namespace XNode.Windows.ImageEditor.ImageRecognition
{
    public class ImageRecognitionEngine : IImageRecognitionEngine
    {
        public ImageRecognitionEngine()
        {
        }

        public async Task<MatchResult?> FindTemplateAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Console.WriteLine($"开始模板匹配，源图像尺寸: {sourceImage.Width}x{sourceImage.Height}, 模板尺寸: {templateImage.Width}x{templateImage.Height}, 阈值: {config.MatchThreshold}");

                    using var sourceMat = BitmapConverter.ToMat(sourceImage);
                    using var templateMat = BitmapConverter.ToMat(templateImage);

                    // 转换为灰度图以提高匹配速度
                    using var sourceGray = new Mat();
                    using var templateGray = new Mat();
                    Cv2.CvtColor(sourceMat, sourceGray, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(templateMat, templateGray, ColorConversionCodes.BGR2GRAY);

                    // 执行模板匹配
                    using var result = new Mat();
                    Cv2.MatchTemplate(sourceGray, templateGray, result, TemplateMatchModes.CCoeffNormed);

                    // 找到最佳匹配位置
                    double minVal, maxVal;
                    OpenCvSharp.Point minLoc, maxLoc;
                    Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);

                    if (maxVal >= config.MatchThreshold)
                    {
                        // maxLoc是匹配矩形的左上角坐标
                        var matchRect = new Rectangle(maxLoc.X, maxLoc.Y, templateImage.Width, templateImage.Height);

                        // 计算中心点坐标
                        var centerX = maxLoc.X + templateImage.Width / 2;
                        var centerY = maxLoc.Y + templateImage.Height / 2;

                        var matchResult = new MatchResult
                        {
                            Confidence = maxVal,
                            Location = new System.Drawing.Point(centerX, centerY),  // 存储中心点坐标
                            Rectangle = matchRect,  // 存储完整矩形边界
                            MatchMethod = "TemplateMatching",
                            ElapsedMilliseconds = 0
                        };

                        Console.WriteLine($"模板匹配成功，置信度: {maxVal:P2}, 左上角: ({maxLoc.X}, {maxLoc.Y}), 中心点: ({centerX}, {centerY})");
                        return matchResult;
                    }

                    Console.WriteLine($"模板匹配失败，最大置信度: {maxVal:P2} < 阈值 {config.MatchThreshold}");
                    return null;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"模板匹配异常: {ex.Message}");
                    return null;
                }
            }, cancellationToken);
        }

        public async Task<List<MatchResult>> FindAllTemplatesAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // 简化实现：仅返回单个匹配
                var singleMatch = FindTemplateAsync(sourceImage, templateImage, config, cancellationToken).Result;
                return singleMatch != null ? new List<MatchResult> { singleMatch } : new List<MatchResult>();
            }, cancellationToken);
        }

        public async Task<MatchResult?> FindByFeaturesAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // 占位实现：使用模板匹配作为 fallback
                Console.WriteLine("特征匹配未实现，使用模板匹配作为 fallback");
                return FindTemplateAsync(sourceImage, templateImage, config, cancellationToken).Result;
            }, cancellationToken);
        }

        public async Task<bool> IsImagePresentAsync(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var config = new ImageRecognitionConfig { MatchThreshold = threshold };
                var result = FindTemplateAsync(sourceImage, templateImage, config, cancellationToken).Result;
                return result != null;
            }, cancellationToken);
        }

        public async Task<double> CalculateSimilarityAsync(Bitmap image1, Bitmap image2)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var mat1 = BitmapConverter.ToMat(image1);
                    using var mat2 = BitmapConverter.ToMat(image2);

                    using var gray1 = new Mat();
                    using var gray2 = new Mat();
                    Cv2.CvtColor(mat1, gray1, ColorConversionCodes.BGR2GRAY);
                    Cv2.CvtColor(mat2, gray2, ColorConversionCodes.BGR2GRAY);

                    // 调整大小以确保相同尺寸
                    Cv2.Resize(gray2, gray2, gray1.Size());

                    using var diff = new Mat();
                    Cv2.Absdiff(gray1, gray2, diff);

                    var mean = Cv2.Mean(diff)[0];
                    var similarity = 1.0 - (mean / 255.0);

                    Console.WriteLine($"图像相似度计算: {similarity:P2}");
                    return similarity;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"图像相似度计算异常: {ex.Message}");
                    return 0.0;
                }
            });
        }

        public async Task<MatchResult?> FindTemplateInRegionAsync(Bitmap sourceImage, Bitmap templateImage, Rectangle region, ImageRecognitionConfig config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // 裁剪源图像到指定区域
                    using var croppedSource = new Bitmap(region.Width, region.Height);
                    using var g = Graphics.FromImage(croppedSource);
                    g.DrawImage(sourceImage, new Rectangle(0, 0, region.Width, region.Height), region, GraphicsUnit.Pixel);

                    // 在裁剪后的图像中查找模板（返回的是相对于区域的坐标）
                    var result = FindTemplateAsync(croppedSource, templateImage, config, CancellationToken.None).Result;

                    // 如果找到匹配，将相对坐标转换为屏幕绝对坐标
                    if (result != null)
                    {
                        // 将相对坐标转换为屏幕绝对坐标
                        result.Location = new System.Drawing.Point(
                            result.Location.X + region.X,
                            result.Location.Y + region.Y
                        );

                        result.Rectangle = new Rectangle(
                            result.Rectangle.X + region.X,
                            result.Rectangle.Y + region.Y,
                            result.Rectangle.Width,
                            result.Rectangle.Height
                        );

                        Console.WriteLine($"区域查找成功，相对坐标已转换为屏幕坐标: ({result.Location.X}, {result.Location.Y})");
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"区域模板匹配异常: {ex.Message}");
                    return null;
                }
            }, cancellationToken);
        }

        public async Task<MatchResult?> FindByFeatureMatchingAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                // 占位实现：使用模板匹配
                Console.WriteLine("特征匹配未实现，使用模板匹配作为 fallback");
                return FindTemplateAsync(sourceImage, templateImage, config, cancellationToken).Result;
            }, cancellationToken);
        }
    }
}
