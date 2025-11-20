using NodeLib.Automation.Input;
using NodeLib.Automation.Vision;
using NodeLib.Automation.Control;
using XLib.Base.Ex;
using XLib.Base.VirtualDisk;
using XLib.Node;

namespace NodeLib.Automation
{
    /// <summary>
    /// 自动化节点库管理器
    /// </summary>
    public class AutomationLib : INodeLib
    {
        #region 单例

        private AutomationLib() { }
        public static AutomationLib Instance { get; } = new AutomationLib();

        #endregion

        #region INodeLib 属性

        public string Name { get; set; } = "Automation";

        public string Title { get; set; } = "自动化";

        public Harddisk LibHarddisk { get; set; } = new Harddisk();

        #endregion

        #region INodeLib 方法

        public void Init()
        {
            // 创建文件夹分类
            Folder inputFolder = LibHarddisk.CreateFolder("输入操作".PackToList());
            Folder visionFolder = LibHarddisk.CreateFolder("视觉识别".PackToList());
            Folder controlFolder = LibHarddisk.CreateFolder("流程控制".PackToList());

            // 输入操作节点
            LibHarddisk.CreateFile(inputFolder, "鼠标点击", "nt", new NodeType<MouseClickNode>());
            LibHarddisk.CreateFile(inputFolder, "鼠标移动", "nt", new NodeType<MouseMoveNode>());

            // 视觉识别节点
            LibHarddisk.CreateFile(visionFolder, "屏幕截图", "nt", new NodeType<CaptureScreenNode>());
            LibHarddisk.CreateFile(visionFolder, "查找图像", "nt", new NodeType<FindImageNode>());

            // 流程控制节点
            LibHarddisk.CreateFile(controlFolder, "延迟", "nt", new NodeType<DelayNode>());
        }

        public void Clear() { }

        public NodeBase? CreateNode(string typeString)
        {
            return typeString switch
            {
                nameof(MouseClickNode) => new MouseClickNode(),
                nameof(MouseMoveNode) => new MouseMoveNode(),
                nameof(CaptureScreenNode) => new CaptureScreenNode(),
                nameof(FindImageNode) => new FindImageNode(),
                nameof(DelayNode) => new DelayNode(),
                _ => null,
            };
        }

        #endregion
    }
}
