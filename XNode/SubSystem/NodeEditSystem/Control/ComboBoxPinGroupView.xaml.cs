using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using XLib.Node;
using XNode.SubSystem.NodeEditSystem.Define;
using XNode.SubSystem.ResourceSystem;
using WpfPoint = System.Windows.Point;
using WpfColor = System.Windows.Media.Color;
using WpfMouseEventArgs = System.Windows.Input.MouseEventArgs;

namespace XNode.SubSystem.NodeEditSystem.Control
{
    /// <summary>
    /// 下拉选择框引脚组视图
    /// </summary>
    public partial class ComboBoxPinGroupView : PinGroupViewBase
    {
        #region 属性

        public ComboBoxPinGroup? Instance { get; set; }

        #endregion

        #region 构造函数

        public ComboBoxPinGroupView()
        {
            InitializeComponent();
        }

        #endregion

        #region 基类方法

        public override void Init()
        {
            if (Instance == null) return;

            Block_Name.Text = Instance.Name;
            Block_Name.Foreground = new SolidColorBrush(GetDataPinColor());

            // 加载选项列表
            ComboBox_Value.Items.Clear();
            foreach (var option in Instance.Options)
            {
                var displayName = Instance.GetDisplayName(option);
                ComboBox_Value.Items.Add(displayName);
            }

            // 设置当前选中项
            if (Instance.Options.Count > 0)
            {
                var currentIndex = Instance.Options.IndexOf(Instance.Value);
                if (currentIndex >= 0)
                    ComboBox_Value.SelectedIndex = currentIndex;
                else
                    ComboBox_Value.SelectedIndex = 0;
            }

            ComboBox_Value.IsEnabled = Instance.CanInput;
            InputBoxArea.Width = new GridLength(Instance.BoxWidth);

            // 无输入引脚：隐藏引脚图标与区域
            if (Instance.InputPin == null)
            {
                Icon_LeftPin.Visibility = Visibility.Collapsed;
                LeftPinArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                Icon_LeftPin.Source = GetDataPinIcon();
                LeftPinArea.MouseEnter += LeftPinArea_MouseEnter;
                LeftPinArea.MouseLeave += PinArea_MouseLeave;
            }

            // 无输出引脚：隐藏引脚图标与区域
            if (Instance.OutputPin == null)
            {
                Icon_RightPin.Visibility = Visibility.Collapsed;
                RightPinArea.Visibility = Visibility.Collapsed;
            }
            else
            {
                Icon_RightPin.Source = GetDataPinIcon();
                RightPinArea.MouseEnter += RightPinArea_MouseEnter;
                RightPinArea.MouseLeave += PinArea_MouseLeave;
            }

            Instance.ValueChanged += ValueChanged;
        }

        public override Grid GetPinArea()
        {
            if (Instance?.InputPin != null && HoveredPin == Instance.InputPin) return LeftPinArea;
            if (Instance?.OutputPin != null && HoveredPin == Instance.OutputPin) return RightPinArea;
            throw new Exception("无命中引脚");
        }

        public override WpfPoint GetPinOffset(NodeView card, int pinIndex)
        {
            if (pinIndex == 0) return LeftPinArea.TranslatePoint(new WpfPoint(3, 8), card);
            return RightPinArea.TranslatePoint(new WpfPoint(14, 8), card);
        }

        public override void UpdatePinIcon()
        {
            if (Instance?.InputPin != null)
                Icon_LeftPin.Source = GetDataPinIcon(Instance.InputPin.SourceList.Count > 0);
            if (Instance?.OutputPin != null)
                Icon_RightPin.Source = GetDataPinIcon(Instance.OutputPin.TargetList.Count > 0);
        }

        #endregion

        #region 事件处理

        private void ComboBox_Value_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Instance != null && ComboBox_Value.SelectedIndex >= 0 && ComboBox_Value.SelectedIndex < Instance.Options.Count)
            {
                var selectedValue = Instance.Options[ComboBox_Value.SelectedIndex];
                if (Instance.Value != selectedValue)
                    Instance.SetValue(selectedValue);
            }
        }

        private void LeftPinArea_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = Instance?.InputPin;
        }

        private void RightPinArea_MouseEnter(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = Instance?.OutputPin;
        }

        private void PinArea_MouseLeave(object sender, WpfMouseEventArgs e)
        {
            HoveredPin = null;
        }

        #endregion

        #region 私有方法

        private WpfColor GetDataPinColor()
        {
            return Instance?.Type switch
            {
                "int" => PinColorSet.Int,
                "double" => PinColorSet.Double,
                "string" => PinColorSet.String,
                "bool" => PinColorSet.Bool,
                "byte[]" => PinColorSet.ByteArray,
                _ => Colors.White,
            };
        }

        private BitmapSource? GetDataPinIcon(bool solid = false)
        {
            if (Instance == null) return null;

            return Instance.Type switch
            {
                "int" or "double" or "string" or "bool" or "byte[]" =>
                    PinIconManager.Instance.GetDataPinIcon(Instance.Type, solid),
                _ => null,
            };
        }

        private void ValueChanged()
        {
            Dispatcher.Invoke(() =>
            {
                if (Instance != null && Instance.Options.Count > 0)
                {
                    var currentIndex = Instance.Options.IndexOf(Instance.Value);
                    if (currentIndex >= 0)
                        ComboBox_Value.SelectedIndex = currentIndex;
                }
            });
        }

        #endregion
    }
}
