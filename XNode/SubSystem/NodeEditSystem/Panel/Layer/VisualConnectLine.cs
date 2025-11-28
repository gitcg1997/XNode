using System.Windows;
using System.Windows.Media;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfPen = System.Windows.Media.Pen;
using XLib.Node;
using XLib.WPF.Drawing;

namespace XNode.SubSystem.NodeEditSystem.Panel.Layer
{
    public class VisualConnectLine : VisualElement
    {
        public VisualConnectLine() => _penExecute.Freeze();

        public PinBase StartPin { get; set; }

        public PinBase EndPin { get; set; }

        public WpfPoint Start { get; set; }

        public WpfPoint End { get; set; }

        public WpfColor Color { get; set; } = Colors.White;

        public bool IsData { get; set; } = false;

        protected override void OnUpdate(DrawingContext context)
        {
            // 计算连接线区域
            _left = Start.X;
            _right = End.X;
            _top = Start.Y + 0.5;
            _bottom = End.Y + 0.5;

            // 计算贝塞尔曲线的控制线长度
            double controlLineLength = (_right - _left) / 2;
            if (controlLineLength < _minLength) controlLineLength = _minLength;

            // 创建形状
            PathGeometry geometry = new PathGeometry();
            PathFigure figure = new PathFigure();
            geometry.Figures.Add(figure);

            // 计算贝塞尔曲线的控制点与终点
            WpfPoint p1 = new WpfPoint(_left + controlLineLength, _top);
            WpfPoint p2 = new WpfPoint(_right - controlLineLength, _bottom);
            WpfPoint endPoint = new WpfPoint(_right, _bottom);

            // 设置起点并添加贝塞尔曲线
            figure.StartPoint = new WpfPoint(_left, _top);
            figure.Segments.Add(new BezierSegment(p1, p2, endPoint, true));

            WpfPen pen = new WpfPen(new SolidColorBrush(Color), 1);
            // 绘制形状
            if (!IsData) context.DrawGeometry(null, _penExecute, geometry);
            else context.DrawGeometry(null, new WpfPen(new SolidColorBrush(Color), 1), geometry);
        }

        public override string ToString() => $"{Start}-{End}";

        private double _left = 0;
        private double _right = 0;
        private double _top = 0;
        private double _bottom = 0;

        /// <summary>控制线最短长度</summary>
        private readonly int _minLength = 40;

        /// <summary>执行引脚线</summary>
        private readonly WpfPen _penExecute = new WpfPen(new SolidColorBrush(Color.FromArgb(255, 196, 126, 255)), 1);
    }
}