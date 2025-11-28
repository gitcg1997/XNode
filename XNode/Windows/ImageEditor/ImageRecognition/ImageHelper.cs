using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace XNode.Windows.ImageEditor.ImageRecognition;

/// <summary>
/// 图像处理辅助工具类
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// 将 Bitmap 转换为 OpenCV Mat
    /// </summary>
    /// <param name="bitmap">Bitmap图像</param>
    /// <returns>OpenCV Mat对象</returns>
    public static Mat BitmapToMat(Bitmap bitmap)
    {
        try
        {
            return BitmapConverter.ToMat(bitmap);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to convert Bitmap to Mat: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 将 OpenCV Mat 转换为 Bitmap
    /// </summary>
    /// <param name="mat">OpenCV Mat对象</param>
    /// <returns>Bitmap图像</returns>
    public static Bitmap MatToBitmap(Mat mat)
    {
        try
        {
            return BitmapConverter.ToBitmap(mat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to convert Mat to Bitmap: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 从文件加载图像为 Mat
    /// </summary>
    /// <param name="filePath">图像文件路径</param>
    /// <param name="flags">加载标志</param>
    /// <returns>OpenCV Mat对象</returns>
    public static Mat LoadImage(string filePath, ImreadModes flags = ImreadModes.Color)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Image file not found: {filePath}");
            }

            Mat mat = Cv2.ImRead(filePath, flags);
            if (mat.Empty())
            {
                throw new InvalidOperationException($"Failed to load image from: {filePath}");
            }

            return mat;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load image from {filePath}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 保存 Mat 图像到文件
    /// </summary>
    /// <param name="mat">OpenCV Mat对象</param>
    /// <param name="filePath">保存路径</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveImage(Mat mat, string filePath)
    {
        try
        {
            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return Cv2.ImWrite(filePath, mat);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save image to {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 保存 Bitmap 图像到文件
    /// </summary>
    /// <param name="bitmap">Bitmap图像</param>
    /// <param name="filePath">保存路径</param>
    /// <param name="format">图像格式</param>
    /// <returns>是否保存成功</returns>
    public static bool SaveBitmap(Bitmap bitmap, string filePath, ImageFormat? format = null)
    {
        try
        {
            format ??= ImageFormat.Png;

            string? directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            bitmap.Save(filePath, format);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to save bitmap to {filePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 缩放图像
    /// </summary>
    /// <param name="source">源图像</param>
    /// <param name="scale">缩放比例</param>
    /// <returns>缩放后的图像</returns>
    public static Mat ResizeImage(Mat source, double scale)
    {
        try
        {
            if (scale <= 0)
            {
                throw new ArgumentException("Scale must be greater than 0", nameof(scale));
            }

            if (Math.Abs(scale - 1.0) < 0.001)
            {
                return source.Clone();
            }

            int newWidth = (int)(source.Width * scale);
            int newHeight = (int)(source.Height * scale);

            Mat resized = new Mat();
            Cv2.Resize(source, resized, new OpenCvSharp.Size(newWidth, newHeight), 0, 0, InterpolationFlags.Linear);
            return resized;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to resize image with scale {scale}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 缩放图像到指定尺寸
    /// </summary>
    /// <param name="source">源图像</param>
    /// <param name="width">目标宽度</param>
    /// <param name="height">目标高度</param>
    /// <returns>缩放后的图像</returns>
    public static Mat ResizeImage(Mat source, int width, int height)
    {
        try
        {
            Mat resized = new Mat();
            Cv2.Resize(source, resized, new OpenCvSharp.Size(width, height), 0, 0, InterpolationFlags.Linear);
            return resized;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to resize image to {width}x{height}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 裁剪图像
    /// </summary>
    /// <param name="source">源图像</param>
    /// <param name="rect">裁剪区域</param>
    /// <returns>裁剪后的图像</returns>
    public static Mat CropImage(Mat source, Rectangle rect)
    {
        try
        {
            // 验证裁剪区域是否在图像范围内
            if (rect.X < 0 || rect.Y < 0 ||
                rect.X + rect.Width > source.Width ||
                rect.Y + rect.Height > source.Height)
            {
                throw new ArgumentException("Crop region is out of image bounds", nameof(rect));
            }

            OpenCvSharp.Rect cvRect = new OpenCvSharp.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            return new Mat(source, cvRect);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to crop image at {rect}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 裁剪 Bitmap 图像
    /// </summary>
    /// <param name="source">源图像</param>
    /// <param name="rect">裁剪区域</param>
    /// <returns>裁剪后的图像</returns>
    public static Bitmap CropBitmap(Bitmap source, Rectangle rect)
    {
        try
        {
            // 验证裁剪区域
            if (rect.X < 0 || rect.Y < 0 ||
                rect.X + rect.Width > source.Width ||
                rect.Y + rect.Height > source.Height)
            {
                throw new ArgumentException("Crop region is out of image bounds", nameof(rect));
            }

            Bitmap cropped = new Bitmap(rect.Width, rect.Height);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.DrawImage(source, new Rectangle(0, 0, rect.Width, rect.Height),
                    rect, GraphicsUnit.Pixel);
            }

            return cropped;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to crop bitmap at {rect}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 转换为灰度图像
    /// </summary>
    /// <param name="source">源图像</param>
    /// <returns>灰度图像</returns>
    public static Mat ConvertToGray(Mat source)
    {
        try
        {
            if (source.Channels() == 1)
            {
                return source.Clone();
            }

            Mat gray = new Mat();
            Cv2.CvtColor(source, gray, ColorConversionCodes.BGR2GRAY);
            return gray;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to convert image to grayscale: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 克隆图像
    /// </summary>
    /// <param name="source">源图像</param>
    /// <returns>克隆的图像</returns>
    public static Mat CloneImage(Mat source)
    {
        return source.Clone();
    }

    /// <summary>
    /// 获取图像信息
    /// </summary>
    /// <param name="mat">图像</param>
    /// <returns>图像信息字符串</returns>
    public static string GetImageInfo(Mat mat)
    {
        return $"Size: {mat.Width}x{mat.Height}, Channels: {mat.Channels()}, Depth: {mat.Depth()}, Type: {mat.Type()}";
    }

    /// <summary>
    /// 验证图像是否有效
    /// </summary>
    /// <param name="mat">图像</param>
    /// <returns>是否有效</returns>
    public static bool IsValidImage(Mat? mat)
    {
        return mat != null && !mat.Empty() && mat.Width > 0 && mat.Height > 0;
    }

    /// <summary>
    /// 绘制矩形到图像上
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="rect">矩形区域</param>
    /// <param name="color">颜色</param>
    /// <param name="thickness">线条粗细</param>
    public static void DrawRectangle(Mat image, Rectangle rect, Scalar color, int thickness = 2)
    {
        try
        {
            OpenCvSharp.Rect cvRect = new OpenCvSharp.Rect(rect.X, rect.Y, rect.Width, rect.Height);
            Cv2.Rectangle(image, cvRect, color, thickness);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to draw rectangle on image: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 在图像上绘制文本
    /// </summary>
    /// <param name="image">图像</param>
    /// <param name="text">文本内容</param>
    /// <param name="position">位置</param>
    /// <param name="color">颜色</param>
    /// <param name="fontSize">字体大小</param>
    public static void DrawText(Mat image, string text, OpenCvSharp.Point position, Scalar color, double fontSize = 1.0)
    {
        try
        {
            Cv2.PutText(image, text, position, HersheyFonts.HersheySimplex, fontSize, color, 2);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to draw text on image: {ex.Message}");
            throw;
        }
    }
}
