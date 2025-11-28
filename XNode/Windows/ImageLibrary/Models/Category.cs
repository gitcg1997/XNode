using System;

namespace XNode.Windows.ImageLibrary.Models
{
    /// <summary>
    /// 图片分类模型
    /// </summary>
    public class Category
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 分类描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 分类图标 (Material Design Icon Kind)
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// 分类颜色
        /// </summary>
        public string Color { get; set; } = "#1976D2";

        /// <summary>
        /// 图片数量
        /// </summary>
        public int ImageCount { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// 是否系统分类
        /// </summary>
        public bool IsSystem { get; set; }
    }
}
