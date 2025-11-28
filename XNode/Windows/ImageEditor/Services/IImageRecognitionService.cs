using XNode.Windows.ImageEditor.Models;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace XNode.Windows.ImageEditor.Services.Interfaces;

/// <summary>
/// 图像识别服务接口
/// </summary>
public interface IImageRecognitionService
{
    #region 旧API兼容性方法

    /// <summary>
    /// 在屏幕上查找图像（返回中心点坐标）
    /// </summary>
    System.Windows.Point? FindImageOnScreen(string templateImagePath, double threshold = 0.8, bool useGrayscale = true, bool useSystemScaling = true);

    /// <summary>
    /// 在屏幕上查找图像（返回详细匹配结果）
    /// </summary>
    ImageMatchResult? FindImageOnScreenWithDetails(string templateImagePath, double threshold = 0.8, bool useGrayscale = true, bool useSystemScaling = true);

    /// <summary>
    /// 显示闪烁效果
    /// </summary>
    void ShowBlinkEffect(System.Windows.Point location, double width = 50, double height = 50, Action? onBlinkCompleted = null);

    #endregion

    #region 新API方法

    /// <summary>
    /// 在屏幕上查找图像（异步）
    /// </summary>
    Task<MatchResult?> FindImageOnScreenAsync(string templateImagePath, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在屏幕上查找图像（异步，使用Bitmap）
    /// </summary>
    Task<MatchResult?> FindImageOnScreenAsync(Bitmap templateImage, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在屏幕上查找所有匹配的图像
    /// </summary>
    Task<List<MatchResult>> FindAllImagesOnScreenAsync(string templateImagePath, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在指定窗口中查找图像
    /// </summary>
    Task<MatchResult?> FindImageInWindowAsync(IntPtr windowHandle, string templateImagePath, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在指定区域中查找图像
    /// </summary>
    Task<MatchResult?> FindImageInRegionAsync(string templateImagePath, Rectangle searchRegion, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 等待图像出现
    /// </summary>
    Task<MatchResult?> WaitForImageAsync(string templateImagePath, int timeout = 10000, int checkInterval = 500, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 等待图像消失
    /// </summary>
    Task<bool> WaitForImageDisappearAsync(string templateImagePath, int timeout = 10000, int checkInterval = 500, ImageRecognitionConfig? config = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 计算两张图片的相似度
    /// </summary>
    Task<double> CalculateSimilarityAsync(string imagePath1, string imagePath2);

    /// <summary>
    /// 检查图像是否在屏幕上
    /// </summary>
    Task<bool> IsImageOnScreenAsync(string templateImagePath, ImageRecognitionConfig? config = null);

    #endregion
}
