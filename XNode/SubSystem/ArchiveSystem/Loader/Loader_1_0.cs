using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using XLib.Node;
using XNode.SubSystem.ArchiveSystem.Define.Data_1_0;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.NodeLibSystem;
using XNode.SubSystem.WindowSystem;

namespace XNode.SubSystem.ArchiveSystem.Loader
{
    public class Loader_1_0
    {
        public static bool Import(Data_1_0 data, string archiveFilePath)
        {
            MainWindow.LogManager.LogInfo($"开始加载存档: {System.IO.Path.GetFileName(archiveFilePath)}");

            try
            {
                // 验证存档数据
                if (!ValidateArchiveData(data))
                {
                    MainWindow.LogManager.LogError("存档数据验证失败");
                    WM.ShowError("加载存档失败: 存档数据格式无效");
                    return false;
                }

                // 显示加载进度
                int totalNodes = data.NodeList.Count;
                int totalConnections = data.ConnectLineList.Count;
                MainWindow.LogManager.LogInfo($"准备加载 {totalNodes} 个节点和 {totalConnections} 个连接");

                int nodeCount = LoadNodeList(data);
                if (nodeCount == 0)
                {
                    MainWindow.LogManager.LogWarning("存档中没有可用的节点");
                }
                else
                {
                    MainWindow.LogManager.LogInfo($"成功加载 {nodeCount} 个节点");
                }

                int connectionCount = LoadConnectLineList(data);
                if (connectionCount > 0)
                {
                    MainWindow.LogManager.LogInfo($"成功加载 {connectionCount} 个连接线");
                }

                MainWindow.LogManager.LogInfo("存档加载完成");
                return true;
            }
            catch (JsonException ex)
            {
                MainWindow.LogManager.LogError($"JSON解析失败: {ex.Message}");
                WM.ShowError($"加载存档失败: JSON格式错误\n{ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                MainWindow.LogManager.LogError($"加载存档失败: {ex.Message}");
                MainWindow.LogManager.LogError($"堆栈跟踪: {ex.StackTrace}");
                WM.ShowError($"加载存档失败: {ex.Message}\n\n详细信息请查看日志");
                return false;
            }
        }

        /// <summary>
        /// 验证存档数据
        /// </summary>
        private static bool ValidateArchiveData(Data_1_0 data)
        {
            if (data == null)
            {
                MainWindow.LogManager.LogError("存档数据为空");
                return false;
            }

            if (data.NodeList == null)
            {
                MainWindow.LogManager.LogError("节点列表为空");
                return false;
            }

            if (data.ConnectLineList == null)
            {
                MainWindow.LogManager.LogError("连接线列表为空");
                return false;
            }

            MainWindow.LogManager.LogInfo($"存档数据验证通过: {data.NodeList.Count} 个节点, {data.ConnectLineList.Count} 个连接线");
            return true;
        }

        /// <summary>
        /// 验证节点数据
        /// </summary>
        private static bool ValidateNodeData(NodeData nodeData)
        {
            if (nodeData == null)
                return false;

            if (nodeData.BaseData == null)
            {
                MainWindow.LogManager.LogWarning("节点基本数据为空");
                return false;
            }

            if (nodeData.ParaDict == null || nodeData.PropertyDict == null)
            {
                MainWindow.LogManager.LogWarning("节点参数或属性字典为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 验证连接线数据
        /// </summary>
        private static bool ValidateConnectLineData(ConnectLineData lineData)
        {
            if (lineData == null)
                return false;

            if (string.IsNullOrEmpty(lineData.Start))
            {
                MainWindow.LogManager.LogWarning("连接线起始引脚路径为空");
                return false;
            }

            if (string.IsNullOrEmpty(lineData.End))
            {
                MainWindow.LogManager.LogWarning("连接线目标引脚路径为空");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载节点列表
        /// </summary>
        /// <returns>成功加载的节点数量</returns>
        private static int LoadNodeList(Data_1_0 data)
        {
            int successCount = 0;
            int failCount = 0;
            int totalCount = data.NodeList.Count;

            foreach (var nodeData in data.NodeList)
            {
                try
                {
                    // 验证节点数据
                    if (!ValidateNodeData(nodeData))
                    {
                        failCount++;
                        continue;
                    }

                    // 解析节点基本数据 - 直接使用对象
                    NodeBaseData? nodeBaseData = nodeData.BaseData;
                    if (nodeBaseData == null)
                    {
                        failCount++;
                        MainWindow.LogManager.LogWarning("节点基本数据解析失败,跳过");
                        continue;
                    }

                    // 创建节点实例
                    NodeBase? node;
                    if (nodeBaseData.NodeLibName == "Inner")
                        node = NodeLibManager.Instance.CreateNode(nodeBaseData.TypeString);
                    else
                        node = NodeLibManager.Instance.CreateNode(nodeBaseData.NodeLibName, nodeBaseData.TypeString);

                    // 创建节点失败
                    if (node == null)
                    {
                        failCount++;
                        MainWindow.LogManager.LogWarning(
                            $"无法创建节点: {nodeBaseData.NodeLibName}/{nodeBaseData.TypeString} " +
                            $"(ID: {nodeBaseData.ID}), 可能是节点库未加载或节点类型已移除"
                        );
                        continue;
                    }

                    // 初始化节点
                    node.Init();

                    // 设置编号与坐标
                    node.ID = nodeBaseData.ID;
                    node.Point = new NodePoint(nodeBaseData.Point);

                    // 加载参数、属性
                    try
                    {
                        node.LoadParaDict(nodeBaseData.Version, nodeData.ParaDict);
                        node.LoadPropertyDict(nodeBaseData.Version, nodeData.PropertyDict);
                    }
                    catch (Exception ex)
                    {
                        MainWindow.LogManager.LogWarning(
                            $"节点 {nodeBaseData.TypeString} (ID: {nodeBaseData.ID}) " +
                            $"加载参数/属性时出错: {ex.Message}, 使用默认值"
                        );
                        // 继续加载节点,但使用默认参数
                    }

                    // 加载节点至编辑器
                    WM.Main.Editer.LoadNode(node);
                    successCount++;

                    // 每10个节点报告一次进度
                    if (successCount % 10 == 0)
                    {
                        MainWindow.LogManager.LogInfo($"正在加载节点... ({successCount + failCount}/{totalCount})");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    MainWindow.LogManager.LogError($"加载节点时发生异常: {ex.Message}");
                }
            }

            if (failCount > 0)
            {
                MainWindow.LogManager.LogWarning($"共 {failCount} 个节点加载失败");
            }

            return successCount;
        }

        /// <summary>
        /// 加载连接线列表
        /// </summary>
        /// <returns>成功加载的连接线数量</returns>
        private static int LoadConnectLineList(Data_1_0 data)
        {
            int successCount = 0;
            int failCount = 0;

            foreach (var lineData in data.ConnectLineList)
            {
                try
                {
                    // 验证连接线数据
                    if (!ValidateConnectLineData(lineData))
                    {
                        failCount++;
                        continue;
                    }

                    // 解析引脚路径
                    PinPath startPath = PinPath.ParsePinPath(lineData.Start);
                    PinPath endPath = PinPath.ParsePinPath(lineData.End);

                    // 查找引脚
                    PinBase? startPin = WM.Main.Editer.FindPin(startPath);
                    if (startPin == null)
                    {
                        failCount++;
                        MainWindow.LogManager.LogWarning(
                            $"找不到起始引脚: 节点ID={startPath.NodeID}, " +
                            $"引脚组={startPath.GroupIndex}, 引脚={startPath.PinIndex}"
                        );
                        continue;
                    }

                    PinBase? endPin = WM.Main.Editer.FindPin(endPath);
                    if (endPin == null)
                    {
                        failCount++;
                        MainWindow.LogManager.LogWarning(
                            $"找不到目标引脚: 节点ID={endPath.NodeID}, " +
                            $"引脚组={endPath.GroupIndex}, 引脚={endPath.PinIndex}"
                        );
                        continue;
                    }

                    // 验证引脚连接是否有效（基本验证）
                    if (!ValidatePinConnection(startPin, endPin))
                    {
                        failCount++;
                        MainWindow.LogManager.LogWarning(
                            $"引脚连接无效,跳过连接: {lineData.Start} -> {lineData.End}, " +
                            $"起始引脚={startPin.GetType().Name}, 流向={startPin.Flow}, " +
                            $"目标引脚={endPin.GetType().Name}, 流向={endPin.Flow}"
                        );
                        continue;
                    }

                    // 连接引脚
                    startPin.AddTarget(endPin);
                    endPin.AddSource(startPin);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    MainWindow.LogManager.LogError($"连接引脚失败: {lineData?.Start}-{lineData?.End}, 错误: {ex.Message}");
                }
            }

            if (failCount > 0)
            {
                MainWindow.LogManager.LogWarning($"共 {failCount} 个连接线加载失败");
            }

            return successCount;
        }

        /// <summary>
        /// 验证引脚连接是否有效
        /// </summary>
        private static bool ValidatePinConnection(PinBase startPin, PinBase endPin)
        {
            // 检查是否为正确的输入输出方向
            if (startPin.Flow != PinFlow.Output)
            {
                return false;
            }

            if (endPin.Flow != PinFlow.Input)
            {
                return false;
            }

            // 检查引脚类型是否兼容（使用实际类型而不是组类型）
            // ExecutePin 可以连接 ExecutePin（不管组类型是 Execute 还是 Action）
            // DataPin 只能连接 DataPin
            if (startPin.GetType() != endPin.GetType())
            {
                return false;
            }

            return true;
        }
    }
}