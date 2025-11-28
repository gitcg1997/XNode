namespace XLib.Base.ArchiveFrame
{
    /// <summary>
    /// 存档元数据
    /// </summary>
    public class ArchiveMetadata
    {
        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>修改时间</summary>
        public DateTime ModifiedTime { get; set; } = DateTime.Now;

        /// <summary>应用版本</summary>
        public string AppVersion { get; set; } = "1.0.3 Alpha";

        /// <summary>节点数量</summary>
        public int NodeCount { get; set; } = 0;

        /// <summary>连接数量</summary>
        public int ConnectionCount { get; set; } = 0;

        /// <summary>项目描述</summary>
        public string Description { get; set; } = "";

        /// <summary>作者</summary>
        public string Author { get; set; } = "";

        /// <summary>校验和</summary>
        public string Checksum { get; set; } = "";
    }

    /// <summary>
    /// 存档文件
    /// </summary>
    public class ArchiveFile
    {
        /// <summary>存档版本</summary>
        public string Version { get; set; } = "1.0";

        /// <summary>元数据</summary>
        public ArchiveMetadata Metadata { get; set; } = new ArchiveMetadata();

        /// <summary>
        /// 存档数据
        /// 注意: 使用 TypeNameHandling.Auto 确保反序列化时保留类型信息
        /// </summary>
        [Newtonsoft.Json.JsonProperty(TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto)]
        public object? Data { get; set; } = null;
    }
}