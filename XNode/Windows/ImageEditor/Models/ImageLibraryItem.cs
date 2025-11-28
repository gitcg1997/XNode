using System;

namespace XNode.Windows.ImageEditor.Models
{
    /// <summary>
    /// 图像库条目
    /// </summary>
    public class ImageLibraryItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string? ThumbnailPath { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public long FileSize { get; set; }
        public string? Tags { get; set; }  // JSON 格式: ["tag1", "tag2"]
        public string Category { get; set; } = "未分类";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 获取文件大小的人类可读格式
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
        /// 获取图像尺寸描述
        /// </summary>
        public string DimensionsFormatted => $"{Width} × {Height}";
    }
}
