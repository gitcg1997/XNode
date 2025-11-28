using System;

namespace XNode.Windows.ImageLibrary.Models
{
    /// <summary>
    /// 图片库图片项模型
    /// </summary>
    public class ImageItem
    {
        /// <summary>
        /// 图片唯一标识
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 图片名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 图片描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 图片文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 缩略图路径
        /// </summary>
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// 分类ID
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string CategoryName { get; set; } = "未分类";

        /// <summary>
        /// 标签 (JSON数组)
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// 图片宽度
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// 图片高度
        /// </summary>
        public int Height { get; set; }

        /// <summary>
        /// 文件大小(字节)
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 文件格式
        /// </summary>
        public string Format { get; set; } = "PNG";

        /// <summary>
        /// 使用次数
        /// </summary>
        public int UsageCount { get; set; }

        /// <summary>
        /// 最后使用时间
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 是否收藏
        /// </summary>
        public bool IsFavorite { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Notes { get; set; }

        /// <summary>
        /// 格式化的文件大小
        /// </summary>
        public string FileSizeFormatted
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F1} KB";
                else
                    return $"{FileSize / (1024.0 * 1024.0):F1} MB";
            }
        }

        /// <summary>
        /// 图片分辨率
        /// </summary>
        public string Resolution => $"{Width} x {Height}";

        /// <summary>
        /// 相对创建时间
        /// </summary>
        public string RelativeTime
        {
            get
            {
                var span = DateTime.Now - CreatedAt;
                if (span.TotalDays < 1)
                    return $"{(int)span.TotalHours} 小时前";
                else if (span.TotalDays < 30)
                    return $"{(int)span.TotalDays} 天前";
                else if (span.TotalDays < 365)
                    return $"{(int)(span.TotalDays / 30)} 个月前";
                else
                    return $"{(int)(span.TotalDays / 365)} 年前";
            }
        }
    }
}
