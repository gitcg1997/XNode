using System.Drawing;

namespace XNode.Windows.ImageEditor.Models;

/// <summary>
/// 图像匹配结果
/// </summary>
public class MatchResult
{
    /// <summary>
    /// 匹配位置（中心点坐标）
    /// </summary>
    public Point Location { get; set; }

    /// <summary>
    /// 匹配区域（矩形边界）
    /// </summary>
    public Rectangle Rectangle { get; set; }

    /// <summary>
    /// 匹配置信度 (0.0 - 1.0)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 匹配方法
    /// </summary>
    public string? MatchMethod { get; set; }

    /// <summary>
    /// 是否为多尺度匹配
    /// </summary>
    public bool IsMultiScale { get; set; }

    /// <summary>
    /// 匹配时的缩放比例
    /// </summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>
    /// 匹配时间（毫秒）
    /// </summary>
    public long ElapsedMilliseconds { get; set; }

    /// <summary>
    /// 额外信息
    /// </summary>
    public string? AdditionalInfo { get; set; }

    /// <summary>
    /// 模板名称（用于多模板查找时标识匹配来源）
    /// </summary>
    public string? TemplateName { get; set; }

    public override string ToString()
    {
        return $"Location: {Location}, Confidence: {Confidence:P2}, Method: {MatchMethod}, Scale: {Scale:F2}, Template: {TemplateName}";
    }
}
