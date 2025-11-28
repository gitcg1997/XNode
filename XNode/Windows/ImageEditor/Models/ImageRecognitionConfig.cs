namespace XNode.Windows.ImageEditor.Models;

/// <summary>
/// 图像识别配置
/// </summary>
public class ImageRecognitionConfig
{
    /// <summary>
    /// 匹配阈值 (0.0 - 1.0)，默认 0.8
    /// </summary>
    public double MatchThreshold { get; set; } = 0.8;

    /// <summary>
    /// 是否使用灰度图像进行匹配，默认 true
    /// </summary>
    public bool UseGrayscale { get; set; } = true;

    /// <summary>
    /// 是否启用多尺度匹配，默认 false
    /// </summary>
    public bool EnableMultiScale { get; set; } = false;

    /// <summary>
    /// 多尺度匹配的缩放范围（起始），默认 0.5
    /// </summary>
    public double ScaleRangeStart { get; set; } = 0.5;

    /// <summary>
    /// 多尺度匹配的缩放范围（结束），默认 1.5
    /// </summary>
    public double ScaleRangeEnd { get; set; } = 1.5;

    /// <summary>
    /// 多尺度匹配的缩放步长，默认 0.1
    /// </summary>
    public double ScaleStep { get; set; } = 0.1;

    /// <summary>
    /// 是否启用GPU加速（如果可用），默认 false
    /// </summary>
    public bool EnableGpuAcceleration { get; set; } = false;

    /// <summary>
    /// 超时时间（毫秒），0 表示无限制，默认 0
    /// </summary>
    public int TimeoutMs { get; set; } = 0;

    /// <summary>
    /// 是否使用系统缩放设置，默认 true
    /// </summary>
    public bool UseSystemScaling { get; set; } = true;

    /// <summary>
    /// 创建默认配置
    /// </summary>
    public static ImageRecognitionConfig Default => new ImageRecognitionConfig();

    /// <summary>
    /// 创建高精度配置（更严格的阈值）
    /// </summary>
    public static ImageRecognitionConfig HighPrecision => new ImageRecognitionConfig
    {
        MatchThreshold = 0.95,
        UseGrayscale = true,
        EnableMultiScale = false
    };

    /// <summary>
    /// 创建快速配置（更宽松的阈值，使用灰度图）
    /// </summary>
    public static ImageRecognitionConfig Fast => new ImageRecognitionConfig
    {
        MatchThreshold = 0.7,
        UseGrayscale = true,
        EnableMultiScale = false
    };

    /// <summary>
    /// 创建多尺度配置
    /// </summary>
    public static ImageRecognitionConfig MultiScale => new ImageRecognitionConfig
    {
        MatchThreshold = 0.8,
        UseGrayscale = true,
        EnableMultiScale = true,
        ScaleRangeStart = 0.5,
        ScaleRangeEnd = 1.5,
        ScaleStep = 0.1
    };
}
