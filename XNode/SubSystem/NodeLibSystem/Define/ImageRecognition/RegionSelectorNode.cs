using XLib.Node;

namespace XNode.SubSystem.NodeLibSystem.Define.ImageRecognition
{
    /// <summary>
    /// 区域选择节点
    /// 允许用户设置屏幕区域坐标,并将结果输出到数据引脚
    /// </summary>
    public class RegionSelectorNode : NodeBase
    {
        #region 引脚组索引

        private const int PIN_GROUP_EXECUTE_IN = 0;
        private const int PIN_GROUP_REGION_X = 1;
        private const int PIN_GROUP_REGION_Y = 2;
        private const int PIN_GROUP_REGION_WIDTH = 3;
        private const int PIN_GROUP_REGION_HEIGHT = 4;
        private const int PIN_GROUP_EXECUTE_OUT = 5;

        #endregion

        #region 生命周期

        public override void Init()
        {
            SetViewProperty(
                new NodeColor { r = 120, g = 200, b = 255 },
                "CPU",
                "设置区域"
            );

            PinGroupList.Clear();

            PinGroupList.Add(new ExecutePinGroup(this, "Enter"));

            PinGroupList.Add(new DataPinGroup(this, "int", "区域X", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域Y", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 80
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域宽度", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new DataPinGroup(this, "int", "区域高度", "0")
            {
                Writeable = true,
                Readable = true,
                CanInput = true,
                BoxWidth = 100
            });

            PinGroupList.Add(new ActionPinGroup(this, "Next"));

            InitPinGroup();
        }

        #endregion

        #region 节点执行

        protected override void ExecuteNode()
        {
            try
            {
                UpdateData(PIN_GROUP_REGION_X);
                UpdateData(PIN_GROUP_REGION_Y);
                UpdateData(PIN_GROUP_REGION_WIDTH);
                UpdateData(PIN_GROUP_REGION_HEIGHT);

                if (!int.TryParse(GetData(PIN_GROUP_REGION_X), out int x))
                    x = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_Y), out int y))
                    y = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_WIDTH), out int width))
                    width = 0;
                if (!int.TryParse(GetData(PIN_GROUP_REGION_HEIGHT), out int height))
                    height = 0;

                width = Math.Max(0, width);
                height = Math.Max(0, height);

                if (width <= 0 || height <= 0)
                {
                    Console.WriteLine("[RegionSelectorNode] 区域宽度或高度无效");
                }
                else
                {
                    Console.WriteLine($"[RegionSelectorNode] 设置区域: X={x}, Y={y}, W={width}, H={height}");
                }

                var actionGroup = GetPinGroup<ActionPinGroup>(PIN_GROUP_EXECUTE_OUT);
                actionGroup.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RegionSelectorNode] 执行区域设置节点时发生错误: {ex.Message}");
                throw;
            }
        }

        #endregion

        #region 序列化

        public override string GetTypeString() => nameof(RegionSelectorNode);

        public override Dictionary<string, string> GetParaDict()
        {
            return new Dictionary<string, string>
            {
                { "RegionX", GetData(PIN_GROUP_REGION_X) },
                { "RegionY", GetData(PIN_GROUP_REGION_Y) },
                { "RegionWidth", GetData(PIN_GROUP_REGION_WIDTH) },
                { "RegionHeight", GetData(PIN_GROUP_REGION_HEIGHT) }
            };
        }

        protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
        {
            if (paraDict.TryGetValue("RegionX", out string? regionX))
                SetData(PIN_GROUP_REGION_X, regionX);
            if (paraDict.TryGetValue("RegionY", out string? regionY))
                SetData(PIN_GROUP_REGION_Y, regionY);
            if (paraDict.TryGetValue("RegionWidth", out string? regionWidth))
                SetData(PIN_GROUP_REGION_WIDTH, regionWidth);
            if (paraDict.TryGetValue("RegionHeight", out string? regionHeight))
                SetData(PIN_GROUP_REGION_HEIGHT, regionHeight);
        }

        protected override NodeBase CloneNode() => new RegionSelectorNode();

        #endregion
    }
}
