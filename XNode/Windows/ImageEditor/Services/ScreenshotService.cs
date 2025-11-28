using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace XNode.Windows.ImageEditor.Services;

/// <summary>
/// 截屏服务实现
/// </summary>
public class ScreenshotService : IScreenshotService
{
    private readonly ILogger<ScreenshotService> _logger;

    public ScreenshotService(ILogger<ScreenshotService> logger)
    {
        _logger = logger;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    private const int LOGPIXELSX = 88;
    private const int LOGPIXELSY = 90;
    private const uint SRCCOPY = 0x00CC0020;

    /// <inheritdoc/>
    public Task<Bitmap> CaptureScreenAsync()
    {
        return Task.Run(() => CaptureScreenNative());
    }

    /// <inheritdoc/>
    public Task<Bitmap?> CaptureWindowAsync(IntPtr windowHandle)
    {
        return Task.Run(() =>
        {
            try
            {
                if (windowHandle == IntPtr.Zero)
                {
                    _logger.LogWarning("Invalid window handle");
                    return null;
                }

                // 获取窗口矩形
                if (!GetWindowRect(windowHandle, out RECT rect))
                {
                    _logger.LogWarning("Failed to get window rectangle");
                    return null;
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;

                if (width <= 0 || height <= 0)
                {
                    _logger.LogWarning("Invalid window size: {Width}x{Height}", width, height);
                    return null;
                }

                // 获取窗口 DC
                IntPtr windowDC = GetDC(windowHandle);
                IntPtr memDC = CreateCompatibleDC(windowDC);
                IntPtr hBitmap = CreateCompatibleBitmap(windowDC, width, height);
                IntPtr oldBitmap = SelectObject(memDC, hBitmap);

                // 复制窗口内容
                BitBlt(memDC, 0, 0, width, height, windowDC, 0, 0, SRCCOPY);

                // 转换为 .NET Bitmap
                var bitmap = Image.FromHbitmap(hBitmap);

                // 清理资源
                SelectObject(memDC, oldBitmap);
                DeleteObject(hBitmap);
                DeleteDC(memDC);
                ReleaseDC(windowHandle, windowDC);

                _logger.LogDebug("窗口截图完成: {Width}x{Height}", bitmap.Width, bitmap.Height);
                return new Bitmap(bitmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "窗口截图失败");
                return null;
            }
        });
    }

    /// <inheritdoc/>
    public Task<Bitmap> CaptureRegionAsync(Rectangle region)
    {
        return Task.Run(() =>
        {
            try
            {
                // 获取屏幕 DC
                IntPtr screenDC = GetDC(IntPtr.Zero);
                IntPtr memDC = CreateCompatibleDC(screenDC);
                IntPtr hBitmap = CreateCompatibleBitmap(screenDC, region.Width, region.Height);
                IntPtr oldBitmap = SelectObject(memDC, hBitmap);

                // 复制指定区域
                BitBlt(memDC, 0, 0, region.Width, region.Height, screenDC, region.X, region.Y, SRCCOPY);

                // 转换为 .NET Bitmap
                var bitmap = Image.FromHbitmap(hBitmap);

                // 清理资源
                SelectObject(memDC, oldBitmap);
                DeleteObject(hBitmap);
                DeleteDC(memDC);
                ReleaseDC(IntPtr.Zero, screenDC);

                _logger.LogDebug("区域截图完成: {Width}x{Height}", bitmap.Width, bitmap.Height);
                return new Bitmap(bitmap);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "区域截图失败");
                throw;
            }
        });
    }

    /// <summary>
    /// 使用原生 Win32 API 进行全屏截图
    /// </summary>
    private Bitmap CaptureScreenNative()
    {
        try
        {
            // 获取屏幕实际尺寸
            int screenWidth = GetSystemMetrics(0);  // SM_CXSCREEN
            int screenHeight = GetSystemMetrics(1); // SM_CYSCREEN

            _logger.LogDebug("原生截图信息 - 屏幕尺寸: {Width}x{Height}", screenWidth, screenHeight);

            // 获取屏幕 DC
            IntPtr screenDC = GetDC(IntPtr.Zero);
            IntPtr memDC = CreateCompatibleDC(screenDC);
            IntPtr hBitmap = CreateCompatibleBitmap(screenDC, screenWidth, screenHeight);
            IntPtr oldBitmap = SelectObject(memDC, hBitmap);

            // 复制屏幕内容
            BitBlt(memDC, 0, 0, screenWidth, screenHeight, screenDC, 0, 0, SRCCOPY);

            // 转换为 .NET Bitmap
            var bitmap = Image.FromHbitmap(hBitmap);

            // 清理资源
            SelectObject(memDC, oldBitmap);
            DeleteObject(hBitmap);
            DeleteDC(memDC);
            ReleaseDC(IntPtr.Zero, screenDC);

            _logger.LogDebug("原生截图完成: {Width}x{Height}", bitmap.Width, bitmap.Height);

            return new Bitmap(bitmap);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "截图失败");
            throw;
        }
    }

    /// <summary>
    /// 获取系统DPI缩放比例
    /// </summary>
    public double GetDpiScale()
    {
        try
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            int dpiX = GetDeviceCaps(hdc, LOGPIXELSX);
            ReleaseDC(IntPtr.Zero, hdc);
            return dpiX / 96.0; // 96 DPI 是标准DPI
        }
        catch
        {
            return 1.0; // 默认无缩放
        }
    }
}
