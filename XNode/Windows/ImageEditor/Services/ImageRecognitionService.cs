using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using XNode.Windows.ImageEditor.ImageRecognition;
using XNode.Windows.ImageEditor.Models;
using XNode.Windows.ImageEditor.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace XNode.Windows.ImageEditor.Services;

/// <summary>
/// 图像识别服务实现
/// </summary>
public class ImageRecognitionService : IImageRecognitionService
{
    private readonly IImageRecognitionEngine _engine;
    private readonly IScreenshotService _screenshotService;
    private readonly ILogger<ImageRecognitionService> _logger;

    public ImageRecognitionService(
        IImageRecognitionEngine engine,
        IScreenshotService screenshotService,
        ILogger<ImageRecognitionService> logger)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _screenshotService = screenshotService ?? throw new ArgumentNullException(nameof(screenshotService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 旧API兼容性方法

    /// <summary>
    /// 在屏幕上查找图像（返回中心点坐标）- 同步方法，用于向后兼容
    /// </summary>
    public System.Windows.Point? FindImageOnScreen(string templateImagePath, double threshold = 0.8, bool useGrayscale = true, bool useSystemScaling = true)
    {
        // 使用 GetAwaiter().GetResult() 替代 .Result 以避免死锁
        var result = FindImageOnScreenWithDetailsAsync(templateImagePath, threshold, useGrayscale, useSystemScaling).GetAwaiter().GetResult();
        if (result == null) return null;

        // ImageMatchResult.Location 已经是中心点
        return result.Location;
    }

    /// <summary>
    /// 在屏幕上查找图像（返回详细匹配结果）- 同步方法，用于向后兼容
    /// </summary>
    public ImageMatchResult? FindImageOnScreenWithDetails(string templateImagePath, double threshold = 0.8, bool useGrayscale = true, bool useSystemScaling = true)
    {
        return FindImageOnScreenWithDetailsAsync(templateImagePath, threshold, useGrayscale, useSystemScaling).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 在屏幕上查找图像（返回详细匹配结果）- 异步方法
    /// </summary>
    private async Task<ImageMatchResult?> FindImageOnScreenWithDetailsAsync(string templateImagePath, double threshold = 0.8, bool useGrayscale = true, bool useSystemScaling = true)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                _logger.LogWarning("模板图片不存在: {Path}", templateImagePath);
                return null;
            }

            // 截取屏幕
            var screenshot = await _screenshotService.CaptureScreenAsync().ConfigureAwait(false);
            using var templateImage = new Bitmap(templateImagePath);

            // 配置
            var config = new ImageRecognitionConfig
            {
                MatchThreshold = threshold,
                UseGrayscale = useGrayscale,
                UseSystemScaling = useSystemScaling
            };

            // 执行匹配
            var matchResult = await _engine.FindTemplateAsync(screenshot, templateImage, config).ConfigureAwait(false);

            if (matchResult != null)
            {
                // 转换为ImageMatchResult格式
                return new ImageMatchResult
                {
                    Location = new System.Windows.Point(
                        matchResult.Rectangle.X + matchResult.Rectangle.Width / 2.0,
                        matchResult.Rectangle.Y + matchResult.Rectangle.Height / 2.0
                    ),
                    Confidence = matchResult.Confidence,
                    Width = matchResult.Rectangle.Width,
                    Height = matchResult.Rectangle.Height
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "图像识别错误");
            return null;
        }
    }

    /// <summary>
    /// 显示闪烁效果
    /// </summary>
    public void ShowBlinkEffect(System.Windows.Point location, double width = 50, double height = 50, Action? onBlinkCompleted = null)
    {
        try
        {
            Console.WriteLine($"=== ShowBlinkEffect 调用 ===");
            Console.WriteLine($"传入的中心位置: ({location.X:F0}, {location.Y:F0})");
            Console.WriteLine($"传入的尺寸: {width:F0}x{height:F0}");

            // 直接使用BlinkOverlayWindow显示方框
            var blinkWindow = new BlinkOverlayWindow();
            blinkWindow.ShowBlinkAt(location.X, location.Y, width, height, onBlinkCompleted);

            Console.WriteLine($"===========================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ShowBlinkEffect 错误: {ex.Message}");
            onBlinkCompleted?.Invoke();
        }
    }

    #endregion

    #region 新API方法

    /// <summary>
    /// 在屏幕上查找图像（异步）
    /// </summary>
    public async Task<MatchResult?> FindImageOnScreenAsync(
        string templateImagePath,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            using var templateImage = new Bitmap(templateImagePath);
            return await FindImageOnScreenAsync(templateImage, config, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding image on screen from path: {templateImagePath}, {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 在屏幕上查找图像（异步，使用Bitmap）
    /// </summary>
    public async Task<MatchResult?> FindImageOnScreenAsync(
        Bitmap templateImage,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();

            // 截取屏幕
            using var screenshot = await _screenshotService.CaptureScreenAsync().ConfigureAwait(false);
            if (screenshot == null)
            {
                Console.WriteLine("Failed to capture screenshot");
                return null;
            }

            // 执行图像识别
            var effectiveConfig = config ?? ImageRecognitionConfig.Default;
            var result = await _engine.FindTemplateAsync(screenshot, templateImage, effectiveConfig, cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();
            Console.WriteLine($"FindImageOnScreen completed in {stopwatch.ElapsedMilliseconds}ms, Found: {result != null}");

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding image on screen: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 在屏幕上查找所有匹配的图像
    /// </summary>
    public async Task<List<MatchResult>> FindAllImagesOnScreenAsync(
        string templateImagePath,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            using var templateImage = new Bitmap(templateImagePath);
            return await FindAllImagesOnScreenAsync(templateImage, config, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding all images on screen from path: {templateImagePath}, {ex.Message}");
            return new List<MatchResult>();
        }
    }

    /// <summary>
    /// 在屏幕上查找所有匹配的图像（使用Bitmap）
    /// </summary>
    public async Task<List<MatchResult>> FindAllImagesOnScreenAsync(
        Bitmap templateImage,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var screenshot = await _screenshotService.CaptureScreenAsync().ConfigureAwait(false);
            if (screenshot == null)
            {
                Console.WriteLine("Failed to capture screenshot");
                return new List<MatchResult>();
            }

            var effectiveConfig = config ?? ImageRecognitionConfig.Default;
            var results = await _engine.FindAllTemplatesAsync(screenshot, templateImage, effectiveConfig, cancellationToken).ConfigureAwait(false);
            Console.WriteLine($"FindAllImagesOnScreen found {results.Count} matches");

            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding all images on screen: {ex.Message}");
            return new List<MatchResult>();
        }
    }

    /// <summary>
    /// 在指定窗口中查找图像
    /// </summary>
    public async Task<MatchResult?> FindImageInWindowAsync(
        IntPtr windowHandle,
        string templateImagePath,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            // 截取窗口
            using var screenshot = await _screenshotService.CaptureWindowAsync(windowHandle).ConfigureAwait(false);
            if (screenshot == null)
            {
                Console.WriteLine("Failed to capture window screenshot");
                return null;
            }

            using var templateImage = new Bitmap(templateImagePath);
            var effectiveConfig = config ?? ImageRecognitionConfig.Default;
            return await _engine.FindTemplateAsync(screenshot, templateImage, effectiveConfig, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding image in window: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 在指定区域中查找图像
    /// </summary>
    public async Task<MatchResult?> FindImageInRegionAsync(
        string templateImagePath,
        Rectangle searchRegion,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            using var templateImage = new Bitmap(templateImagePath);
            return await FindImageInRegionAsync(templateImage, searchRegion, config, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding image in region from path: {templateImagePath}, {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 在指定区域中查找图像（使用Bitmap）
    /// </summary>
    public async Task<MatchResult?> FindImageInRegionAsync(
        Bitmap templateImage,
        Rectangle searchRegion,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var screenshot = await _screenshotService.CaptureScreenAsync().ConfigureAwait(false);
            if (screenshot == null)
            {
                Console.WriteLine("Failed to capture screenshot");
                return null;
            }

            var effectiveConfig = config ?? ImageRecognitionConfig.Default;
            return await _engine.FindTemplateInRegionAsync(screenshot, templateImage, searchRegion, effectiveConfig, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error finding image in region: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 等待图像出现
    /// </summary>
    public async Task<MatchResult?> WaitForImageAsync(
        string templateImagePath,
        int timeout = 10000,
        int checkInterval = 500,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            var stopwatch = Stopwatch.StartNew();
            using var templateImage = new Bitmap(templateImagePath);

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await FindImageOnScreenAsync(templateImage, config, cancellationToken).ConfigureAwait(false);
                if (result != null)
                {
                    Console.WriteLine($"Image found after {stopwatch.ElapsedMilliseconds}ms");
                    return result;
                }

                await Task.Delay(checkInterval, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"WaitForImage timeout after {timeout}ms");
            return null;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("WaitForImage was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for image: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 等待图像消失
    /// </summary>
    public async Task<bool> WaitForImageDisappearAsync(
        string templateImagePath,
        int timeout = 10000,
        int checkInterval = 500,
        ImageRecognitionConfig? config = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(templateImagePath))
            {
                Console.WriteLine($"Template image file not found: {templateImagePath}");
                throw new FileNotFoundException($"Template image not found: {templateImagePath}");
            }

            var stopwatch = Stopwatch.StartNew();
            using var templateImage = new Bitmap(templateImagePath);

            while (stopwatch.ElapsedMilliseconds < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await FindImageOnScreenAsync(templateImage, config, cancellationToken).ConfigureAwait(false);
                if (result == null)
                {
                    Console.WriteLine($"Image disappeared after {stopwatch.ElapsedMilliseconds}ms");
                    return true;
                }

                await Task.Delay(checkInterval, cancellationToken).ConfigureAwait(false);
            }

            Console.WriteLine($"WaitForImageDisappear timeout after {timeout}ms");
            return false;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("WaitForImageDisappear was cancelled");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error waiting for image to disappear: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 计算两张图片的相似度
    /// </summary>
    public async Task<double> CalculateSimilarityAsync(string imagePath1, string imagePath2)
    {
        try
        {
            if (!File.Exists(imagePath1))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath1}");
            }

            if (!File.Exists(imagePath2))
            {
                throw new FileNotFoundException($"Image file not found: {imagePath2}");
            }

            using var image1 = new Bitmap(imagePath1);
            using var image2 = new Bitmap(imagePath2);

            return await _engine.CalculateSimilarityAsync(image1, image2).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calculating similarity between {imagePath1} and {imagePath2}: {ex.Message}");
            return 0.0;
        }
    }

    /// <summary>
    /// 检查图像是否在屏幕上
    /// </summary>
    public async Task<bool> IsImageOnScreenAsync(
        string templateImagePath,
        ImageRecognitionConfig? config = null)
    {
        try
        {
            var result = await FindImageOnScreenAsync(templateImagePath, config).ConfigureAwait(false);
            return result != null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error checking if image is on screen: {ex.Message}");
            return false;
        }
    }

    #endregion
}

/// <summary>
/// 图像匹配结果（兼容旧API）
/// </summary>
public class ImageMatchResult
{
    public System.Windows.Point Location { get; set; }
    public double Confidence { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
