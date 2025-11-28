using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;
using XLib.WPF.Ex;

namespace XNode.SubSystem.NodeEditSystem.Define
{
    /// <summary>
    /// 引脚颜色集
    /// </summary>
    public class PinColorSet
    {
        public static WpfColor Execute => "C47EFF".ToColor();

        public static WpfColor Bool => "A7C4B5".ToColor();

        public static WpfColor Int => "B3D465".ToColor();

        public static WpfColor Double => "E06C9F".ToColor();

        public static WpfColor String => "F3B562".ToColor();

        public static WpfColor ByteArray => "6CB891".ToColor();
    }
}