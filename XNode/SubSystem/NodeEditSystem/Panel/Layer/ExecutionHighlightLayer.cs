using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using XLib.Animate;
using XLib.WPF.Drawing;
using XNode.SubSystem.NodeEditSystem.Define;
using WpfPoint = System.Windows.Point;
using WpfPen = System.Windows.Media.Pen;
using WpfBrush = System.Windows.Media.Brush;

namespace XNode.SubSystem.NodeEditSystem.Panel.Layer
{
    /// <summary>
    /// 执行高亮图层 - 使用毛玻璃效果
    /// </summary>
    public class ExecutionHighlightLayer : SingleBoard, IMotion
    {
        /// <summary>高亮框</summary>
        public TargetBox? HighlightBox { get; set; } = null;

        /// <summary>高亮透明度</summary>
        public double HighlightOpacity { get; set; } = 0.7;

        /// <summary>脉冲强度</summary>
        public double PulseIntensity { get; set; } = 0.0;

        /// <summary>发光强度</summary>
        public double GlowIntensity { get; set; } = 1.0;

        protected override void OnUpdate()
        {
            if (HighlightBox == null)
            {
                // Console.WriteLine("[ExecutionHighlightLayer] HighlightBox为null，跳过绘制");
                return;
            }

            Console.WriteLine($"[ExecutionHighlightLayer] 开始绘制高亮，透明度: {HighlightOpacity}, 脉冲: {PulseIntensity}, 发光: {GlowIntensity}");

            var rect = new Rect(
                HighlightBox.ScreenPoint.X,
                HighlightBox.ScreenPoint.Y,
                HighlightBox.Width,
                HighlightBox.Height);

            // 创建毛玻璃背景
            WpfBrush glassBrush = CreateGlassBrush();
            _dc.DrawRectangle(glassBrush, null, rect);

            // 创建发光边框
            WpfPen glowPen = CreateGlowPen();
            var glowRect = new Rect(rect.X - 2, rect.Y - 2, rect.Width + 4, rect.Height + 4);
            _dc.DrawRectangle(null, glowPen, glowRect);

            // 创建脉冲效果边框
            if (PulseIntensity > 0)
            {
                WpfPen pulsePen = CreatePulsePen();
                var pulseOffset = PulseIntensity * 5;
                var pulseRect = new Rect(
                    rect.X - pulseOffset,
                    rect.Y - pulseOffset,
                    rect.Width + pulseOffset * 2,
                    rect.Height + pulseOffset * 2);
                _dc.DrawRectangle(null, pulsePen, pulseRect);
            }
            
            Console.WriteLine("[ExecutionHighlightLayer] 高亮绘制完成");
        }

        /// <summary>
        /// 创建毛玻璃画刷
        /// </summary>
        private WpfBrush CreateGlassBrush()
        {
            var gradientBrush = new LinearGradientBrush
            {
                StartPoint = new WpfPoint(0, 0),
                EndPoint = new WpfPoint(1, 1),
                Opacity = HighlightOpacity
            };

            // 使用青蓝色系创建现代感
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(120, 0, 188, 212), 0.0));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(80, 0, 150, 199), 0.5));
            gradientBrush.GradientStops.Add(new GradientStop(Color.FromArgb(40, 0, 119, 182), 1.0));

            return gradientBrush;
        }

        /// <summary>
        /// 创建发光画笔
        /// </summary>
        private WpfPen CreateGlowPen()
        {
            var glowBrush = new SolidColorBrush(Color.FromArgb(200, 0, 188, 212))
            {
                Opacity = GlowIntensity * 0.8
            };
            return new WpfPen(glowBrush, 3);
        }

        /// <summary>
        /// 创建脉冲画笔
        /// </summary>
        private WpfPen CreatePulsePen()
        {
            var pulseBrush = new SolidColorBrush(Color.FromArgb(100, 0, 255, 255))
            {
                Opacity = 1.0 - PulseIntensity
            };
            return new WpfPen(pulseBrush, 2);
        }

        public double GetMotionProperty(string propertyName)
        {
            return propertyName switch
            {
                "HighlightOpacity" => HighlightOpacity,
                "PulseIntensity" => PulseIntensity,
                "GlowIntensity" => GlowIntensity,
                _ => 0
            };
        }

        public void SetMotionProperty(string propertyName, double value)
        {
            switch (propertyName)
            {
                case "HighlightOpacity":
                    HighlightOpacity = Math.Max(0.3, Math.Min(1.0, value));
                    break;
                case "PulseIntensity":
                    PulseIntensity = Math.Max(0.0, Math.Min(1.0, value));
                    break;
                case "GlowIntensity":
                    GlowIntensity = Math.Max(0.0, Math.Min(1.0, value));
                    break;
            }
            Dispatcher.Invoke(Update);
        }
    }
}