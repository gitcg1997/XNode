using System.Windows;
using WpfPoint = System.Windows.Point;

namespace XNode.SubSystem.NodeEditSystem.Define
{
    /// <summary>
    /// 目标框
    /// </summary>
    public class TargetBox
    {
        public WpfPoint ScreenPoint { get; set; }

        public double Height { get; set; }

        public double Width { get; set; }

        /// <summary>外框偏移。正数向外，负数向内</summary>
        public double BoxOffset { get; set; } = 0;

        /// <summary>
        /// 获取绘制目标框的坐标列表。共绘制八条线，每条线两个坐标
        /// </summary>
        public List<WpfPoint> GetPointList(int lineLength)
        {
            List<WpfPoint> result = new List<WpfPoint>();

            double left = ScreenPoint.X - BoxOffset;
            double right = ScreenPoint.X + Width + BoxOffset;
            double top = ScreenPoint.Y - BoxOffset;
            double bottom = ScreenPoint.Y + Height + BoxOffset;

            double hx1 = left + lineLength;
            double hx2 = right - lineLength;
            double hy1 = top + 0.5;
            double hy2 = bottom - 0.5;

            result.Add(new WpfPoint(left, hy1));
            result.Add(new WpfPoint(hx1, hy1));
            result.Add(new WpfPoint(hx2, hy1));
            result.Add(new WpfPoint(right, hy1));

            result.Add(new WpfPoint(left, hy2));
            result.Add(new WpfPoint(hx1, hy2));
            result.Add(new WpfPoint(hx2, hy2));
            result.Add(new WpfPoint(right, hy2));

            double vx1 = left + 0.5;
            double vx2 = right - 0.5;
            double vy1 = top + lineLength;
            double vy2 = bottom - lineLength;

            result.Add(new WpfPoint(vx1, top));
            result.Add(new WpfPoint(vx1, vy1));
            result.Add(new WpfPoint(vx1, vy2));
            result.Add(new WpfPoint(vx1, bottom));

            result.Add(new WpfPoint(vx2, top));
            result.Add(new WpfPoint(vx2, vy1));
            result.Add(new WpfPoint(vx2, vy2));
            result.Add(new WpfPoint(vx2, bottom));

            return result;
        }
    }
}