using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Text;
using XLib.Base.ArchiveFrame;
using XNode.SubSystem.ArchiveSystem.Define.Data_1_0;
using XNode.SubSystem.ArchiveSystem.Loader;
using XNode.SubSystem.WindowSystem;

namespace XNode.SubSystem.ArchiveSystem
{
    /// <summary>
    /// 存档管理器
    /// </summary>
    public class ArchiveManager
    {
        #region 单例

        private ArchiveManager() { }
        public static ArchiveManager Instance { get; } = new ArchiveManager();

        #endregion

        #region 属性

        /// <summary>当前版本</summary>
        public string CurrentVersion { get; private set; } = "1.0";

        #endregion

        #region 公开方法

        /// <summary>
        /// 生成存档
        /// </summary>
        public ArchiveFile GenerateArchive()
        {
            var data = Extracter.Extract();
            var metadata = new XLib.Base.ArchiveFrame.ArchiveMetadata
            {
                CreatedTime = DateTime.Now,
                ModifiedTime = DateTime.Now,
                AppVersion = "1.0.3 Alpha",
                NodeCount = data.NodeList.Count,
                ConnectionCount = data.ConnectLineList.Count,
                Description = "",
                Author = Environment.UserName,
                Checksum = Extracter.CalculateChecksum(data)
            };

            return new ArchiveFile
            {
                Version = CurrentVersion,
                Metadata = metadata,
                Data = data,
            };
        }

        /// <summary>
        /// 读取存档文件
        /// </summary>
        public ArchiveFile? ReadArchiveFile(string filePath)
        {
            try
            {
                string jsonData = File.ReadAllText(filePath, Encoding.UTF8);

                // 使用类型信息进行反序列化,确保 Data 字段正确还原为 Data_1_0 类型
                var settings = new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.Auto,
                    NullValueHandling = NullValueHandling.Ignore,
                    MissingMemberHandling = MissingMemberHandling.Ignore
                };

                var archiveFile = JsonConvert.DeserializeObject<ArchiveFile>(jsonData, settings);

                if (archiveFile == null)
                {
                    MainWindow.LogManager.LogError("反序列化存档文件失败,结果为 null");
                    return null;
                }

                return archiveFile;
            }
            catch (JsonException ex)
            {
                MainWindow.LogManager.LogError($"JSON 解析失败: {ex.Message}");
                return null;
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"读取存档文件失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 导出项目为可读的JSON格式
        /// </summary>
        public bool ExportProjectAsReadableJson(string exportPath)
        {
            try
            {
                MainWindow.LogManager.LogInfo($"开始导出项目为可读JSON格式: {exportPath}");

                var archiveFile = GenerateArchive();
                var settings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    NullValueHandling = NullValueHandling.Ignore,
                    DefaultValueHandling = DefaultValueHandling.Include
                };

                string jsonData = JsonConvert.SerializeObject(archiveFile, settings);
                File.WriteAllText(exportPath, jsonData, Encoding.UTF8);

                MainWindow.LogManager.LogInfo("项目导出成功");
                return true;
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"导出项目失败: {ex.Message}");
                WM.ShowError($"导出项目失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 加载存档
        /// </summary>
        public bool LoadArchive(ArchiveFile file, string path)
        {
            // 检查存档版本
            if (!CheckVersion(file))
            {
                WM.ShowError($"读取存档\"{path}\"失败：无效的版本");
                return false;
            }

            // 比较版本，过新则不加载
            int result = CompareVersion(file.Version);
            if (result < 0)
            {
                WM.ShowTip("存档版本过新，请升级软件后重试");
                return false;
            }

            // 验证存档完整性（如果有校验和）
            if (file.Metadata != null && !string.IsNullOrEmpty(file.Metadata.Checksum))
            {
                if (!ValidateArchiveChecksum(file))
                {
                    MainWindow.LogManager.LogWarning("存档校验和验证失败，文件可能已被修改或损坏");
                    // 继续加载，但记录警告
                }
                else
                {
                    MainWindow.LogManager.LogInfo("存档校验和验证通过");
                }
            }

            // 输出元数据信息
            if (file.Metadata != null)
            {
                MainWindow.LogManager.LogInfo($"存档信息: 版本={file.Metadata.AppVersion}, " +
                    $"节点数={file.Metadata.NodeCount}, 连接数={file.Metadata.ConnectionCount}, " +
                    $"作者={file.Metadata.Author}");
            }

            // 导入存档数据
            if (!ImportArchiveData(file, path))
            {
                WM.ShowError($"存档\"{path}\"加载失败");
                return false;
            }

            return true;
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 验证存档校验和
        /// </summary>
        private bool ValidateArchiveChecksum(ArchiveFile file)
        {
            try
            {
                // 尝试将 Data 转换为 Data_1_0
                Data_1_0? data = null;

                if (file.Data is Data_1_0 data_1_0)
                {
                    data = data_1_0;
                }
                else if (file.Data is JObject jObject)
                {
                    // 兼容旧格式：通过 JObject 转换
                    data = jObject.ToObject<Data_1_0>();
                }

                if (data == null)
                {
                    MainWindow.LogManager.LogWarning("无法提取存档数据进行校验");
                    return false;
                }

                // 计算校验和
                var calculatedChecksum = Extracter.CalculateChecksum(data);

                // 比较校验和
                return string.Equals(calculatedChecksum, file.Metadata?.Checksum, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"验证校验和时发生错误: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查版本
        /// </summary>
        private bool CheckVersion(ArchiveFile file)
        {
            if (string.IsNullOrEmpty(file.Version) || file.Version == "???") return false;
            try
            {
                ArchiveVersion version = new ArchiveVersion(file.Version);
            }
            catch (Exception) { return false; }
            return true;
        }

        /// <summary>
        /// 比较版本
        /// </summary>
        private int CompareVersion(string version)
        {
            ArchiveVersion file = new ArchiveVersion(version);
            ArchiveVersion current = new ArchiveVersion(CurrentVersion);
            return file.CompareTo(current);
        }

        /// <summary>
        /// 导入存档数据
        /// </summary>
        private bool ImportArchiveData(ArchiveFile file, string archiveFilePath)
        {
            try
            {
                switch (file.Version)
                {
                    case "1.0":
                        Data_1_0? data_1_0 = null;

                        // 尝试直接使用强类型数据
                        if (file.Data is Data_1_0 typedData)
                        {
                            data_1_0 = typedData;
                        }
                        // 兼容旧格式：通过 JObject 转换
                        else if (file.Data is JObject jObject)
                        {
                            data_1_0 = ConvertLegacyData(jObject);
                            MainWindow.LogManager.LogInfo("使用兼容模式加载旧版存档文件");
                        }

                        if (data_1_0 == null) return false;
                        if (!Loader_1_0.Import(data_1_0, archiveFilePath)) return false;
                        break;
                    default:
                        return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"导入存档数据失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 转换旧版数据格式
        /// </summary>
        private Data_1_0? ConvertLegacyData(JObject jObject)
        {
            try
            {
                var data = new Data_1_0();

                // 转换节点列表
                var nodeListToken = jObject["NodeList"];
                if (nodeListToken != null)
                {
                    foreach (var item in nodeListToken)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            // 旧格式：字符串需要反序列化
                            var nodeData = JsonConvert.DeserializeObject<NodeData>(item.ToString());
                            if (nodeData != null)
                                data.NodeList.Add(nodeData);
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            // 新格式：直接转换
                            var nodeData = item.ToObject<NodeData>();
                            if (nodeData != null)
                                data.NodeList.Add(nodeData);
                        }
                    }
                }

                // 转换连接线列表
                var connectLineListToken = jObject["ConnectLineList"];
                if (connectLineListToken != null)
                {
                    foreach (var item in connectLineListToken)
                    {
                        if (item.Type == JTokenType.String)
                        {
                            // 旧格式：字符串格式 "start-end"
                            string lineString = item.ToString();
                            string[] parts = lineString.Split('-');
                            if (parts.Length == 2)
                            {
                                data.ConnectLineList.Add(new ConnectLineData
                                {
                                    Start = parts[0],
                                    End = parts[1]
                                });
                            }
                        }
                        else if (item.Type == JTokenType.Object)
                        {
                            // 新格式：直接转换
                            var lineData = item.ToObject<ConnectLineData>();
                            if (lineData != null)
                                data.ConnectLineList.Add(lineData);
                        }
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"转换旧版数据格式失败: {ex.Message}");
                return null;
            }
        }

        #endregion
    }
}