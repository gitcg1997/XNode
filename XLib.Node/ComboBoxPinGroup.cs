namespace XLib.Node
{
    /// <summary>
    /// 下拉选择框引脚组 - 从预定义选项中选择值
    /// </summary>
    public class ComboBoxPinGroup : DataPinGroup
    {
        /// <summary>
        /// 可选项列表
        /// </summary>
        public List<string> Options { get; set; } = new();

        /// <summary>
        /// 选项显示名称映射(key: 值, value: 显示名称)
        /// </summary>
        public Dictionary<string, string> DisplayNames { get; set; } = new();

        public ComboBoxPinGroup(NodeBase node) : base(node)
        {
            GroupType = PinGroupType.ComboBox;
        }

        public ComboBoxPinGroup(NodeBase node, string type, string name, string value)
            : base(node, type, name, value)
        {
            GroupType = PinGroupType.ComboBox;
        }

        /// <summary>
        /// 设置选项列表
        /// </summary>
        public void SetOptions(List<string> options, Dictionary<string, string>? displayNames = null)
        {
            Options = options ?? new List<string>();
            DisplayNames = displayNames ?? new Dictionary<string, string>();

            // 如果当前值不在选项列表中,设置为第一个选项
            if (Options.Count > 0 && !Options.Contains(Value))
            {
                Value = Options[0];
            }
        }

        /// <summary>
        /// 设置值
        /// </summary>
        public void SetValue(string newValue)
        {
            if (Value != newValue)
            {
                Value = newValue;
            }
        }

        /// <summary>
        /// 获取显示名称
        /// </summary>
        public string GetDisplayName(string value)
        {
            return DisplayNames.TryGetValue(value, out string? displayName) ? displayName : value;
        }
    }
}
