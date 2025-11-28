using System.Windows;
using WpfPoint = System.Windows.Point;

namespace XNode.SubSystem.NodeEditSystem.Define
{
    /// <summary>
    /// 连接线
    /// </summary>
    public class ConnectLine
    {
        public WpfPoint Start { get; set; }

        public WpfPoint End { get; set; }
    }
}