namespace XNode.SubSystem.ArchiveSystem.Define.Data_1_0
{
    /// <summary>
    /// 存档数据
    /// </summary>
    public class Data_1_0
    {
        /// <summary>节点列表</summary>
        public List<NodeData> NodeList { get; set; } = new List<NodeData>();

        /// <summary>连接线列表</summary>
        public List<ConnectLineData> ConnectLineList { get; set; } = new List<ConnectLineData>();
    }
}