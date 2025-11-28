namespace XNode.SubSystem.NodeEditSystem.Define
{
    /// <summary>
    /// 引脚路径：支持基于索引(旧格式)和基于名称(新格式)两种方式
    /// </summary>
    public class PinPath
    {
        /// <summary>
        /// 解析引脚路径
        /// </summary>
        public static PinPath ParsePinPath(string path)
        {
            string[] parts = path.Split(',');

            // 判断格式: 如果第三部分是数字,则为旧格式(索引),否则为新格式(名称)
            if (parts.Length == 4 && int.TryParse(parts[2], out int groupIndex))
            {
                // 旧格式: "version,nodeId,groupIndex,pinIndex"
                return new PinPath
                {
                    NodeVersion = parts[0],
                    NodeID = int.Parse(parts[1]),
                    GroupIndex = groupIndex,
                    PinIndex = int.Parse(parts[3]),
                    IsLegacyFormat = true
                };
            }
            else if (parts.Length == 4)
            {
                // 新格式: "version,nodeId,groupName,pinType"
                return new PinPath
                {
                    NodeVersion = parts[0],
                    NodeID = int.Parse(parts[1]),
                    GroupName = parts[2],
                    PinType = parts[3],
                    IsLegacyFormat = false
                };
            }

            throw new FormatException($"无效的引脚路径格式: {path}");
        }

        public string NodeVersion { get; set; } = "1.0";

        public int NodeID { get; set; } = -1;

        // 旧格式(基于索引)
        public int GroupIndex { get; set; } = -1;
        public int PinIndex { get; set; } = -1;

        // 新格式(基于名称)
        public string GroupName { get; set; } = "";
        public string PinType { get; set; } = "";  // "Input" 或 "Output"

        // 标识是否为旧格式
        public bool IsLegacyFormat { get; set; } = false;

        public override string ToString()
        {
            if (IsLegacyFormat)
            {
                // 旧格式
                return $"{NodeVersion},{NodeID},{GroupIndex},{PinIndex}";
            }
            else
            {
                // 新格式
                return $"{NodeVersion},{NodeID},{GroupName},{PinType}";
            }
        }
    }
}