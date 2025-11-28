namespace XLib.Node
{
    /// <summary>
    /// 图像路径引脚组 - 带有浏览、图像库按钮和悬停预览功能
    /// </summary>
    public class ImagePathPinGroup : DataPinGroup
    {
        public ImagePathPinGroup(NodeBase node) : base(node)
        {
            GroupType = PinGroupType.ImagePath;
        }

        public ImagePathPinGroup(NodeBase node, string type, string name, string value)
            : base(node, type, name, value)
        {
            GroupType = PinGroupType.ImagePath;
        }

        /// <summary>
        /// 浏览按钮点击事件
        /// </summary>
        public Action? BrowseButtonClicked { get; set; }

        /// <summary>
        /// 图像库按钮点击事件
        /// </summary>
        public Action? LibraryButtonClicked { get; set; }

        /// <summary>
        /// 设置值并触发更新
        /// </summary>
        public void SetValue(string newValue)
        {
            if (Value != newValue)
            {
                Value = newValue;
            }
        }
    }
}
