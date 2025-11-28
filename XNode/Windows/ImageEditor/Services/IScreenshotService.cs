using System;
using System.Drawing;
using System.Threading.Tasks;

namespace XNode.Windows.ImageEditor.Services;

/// <summary>
/// 截屏服务接口
/// </summary>
public interface IScreenshotService
{
    /// <summary>
    /// 捕获整个屏幕
    /// </summary>
    /// <returns>屏幕截图</returns>
    Task<Bitmap> CaptureScreenAsync();

    /// <summary>
    /// 捕获指定窗口
    /// </summary>
    /// <param name="windowHandle">窗口句柄</param>
    /// <returns>窗口截图</returns>
    Task<Bitmap?> CaptureWindowAsync(IntPtr windowHandle);

    /// <summary>
    /// 捕获屏幕指定区域
    /// </summary>
    /// <param name="region">区域</param>
    /// <returns>区域截图</returns>
    Task<Bitmap> CaptureRegionAsync(Rectangle region);
}
