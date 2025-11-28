using Newtonsoft.Json;

namespace XNode.Windows.ImageEditor.Models
{
    /// <summary>
    /// 图片来源类型
    /// </summary>
    public enum ImageSourceType
    {
        LocalFile,      // 本地文件
        LibraryImage,   // 图片库图片
        Screenshot      // 临时截图
    }

    public class TaskConfig
    {
        [JsonProperty("tasks")]
        public List<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }

    public class TaskItem
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";

        [JsonProperty("template_image")]
        public string TemplateImagePath { get; set; } = "";

        [JsonProperty("image_source_type")]
        public ImageSourceType SourceType { get; set; } = ImageSourceType.LocalFile;

        [JsonProperty("library_image_id")]
        public int? LibraryImageId { get; set; }

        [JsonProperty("library_image_name")]
        public string? LibraryImageName { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; } = "click";

        [JsonProperty("similarity_threshold")]
        public double SimilarityThreshold { get; set; } = 0.8;

        [JsonProperty("on_success")]
        public string OnSuccess { get; set; } = "next";

        [JsonProperty("on_fail")]
        public string OnFail { get; set; } = "retry";

        [JsonProperty("retry_times")]
        public int RetryTimes { get; set; } = 3;

        [JsonProperty("use_system_scaling")]
        public bool UseSystemScaling { get; set; } = true;

        [JsonProperty("use_grayscale_match")]
        public bool UseGrayscaleMatch { get; set; } = false;
    }
}
