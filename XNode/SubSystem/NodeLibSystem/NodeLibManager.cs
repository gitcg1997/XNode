using System.IO;
using System.Reflection;
using XLib.Base;
using XLib.Base.Ex;
using XLib.Base.VirtualDisk;
using XLib.Node;
using XNode.SubSystem.NodeLibSystem.Define.Basics;
using XNode.SubSystem.NodeLibSystem.Define.Actions;
using XNode.SubSystem.NodeLibSystem.Define.Controls;
using XNode.SubSystem.NodeLibSystem.Define.ImageRecognition;
using XNode.SubSystem.OptionSystem;

namespace XNode.SubSystem.NodeLibSystem
{
    public class NodeLibManager : IManager
    {
        #region 单例

        private NodeLibManager() { }
        public static NodeLibManager Instance { get; } = new NodeLibManager();

        #endregion

        #region 属性

        /// <summary>根文件夹</summary>
        public Folder Root => _nodeLibRoot.Root;

        /// <summary>节点库字典</summary>
        public Dictionary<string, INodeLib> NodeLibDict { get; set; } = new Dictionary<string, INodeLib>();

        #endregion

        #region IManager 方法

        public void Init()
        {
            BuildInnerNodeLib();
            LoadOutsideNodeLib();
        }

        public void Reset() { }

        public void Clear() { }

        #endregion

        #region 公开方法

        /// <summary>
        /// 创建节点
        /// </summary>
        public NodeBase? CreateNode(string typeString)
        {
            return typeString switch
            {
                // 基础节点
                nameof(StartNode) => new StartNode(),
                nameof(EndNode) => new EndNode(),

                // 动作节点
                nameof(MouseClickNode) => new MouseClickNode(),
                nameof(DelayNode) => new DelayNode(),

                // 控制流节点
                nameof(ConditionNode) => new ConditionNode(),
                nameof(LoopNode) => new LoopNode(),
                nameof(SmartLoopNode) => new SmartLoopNode(),

                // 图像识别节点
                nameof(ScreenFindImageNode) => new ScreenFindImageNode(),
                nameof(RegionFindImageNode) => new RegionFindImageNode(),
                nameof(RegionSelectorNode) => new RegionSelectorNode(),
                nameof(WaitForImageNode) => new WaitForImageNode(),
                nameof(IntegratedRegionFindNode) => new IntegratedRegionFindNode(),

                _ => null,
            };
        }

        /// <summary>
        /// 创建节点
        /// </summary>
        public NodeBase? CreateNode(string libName, string typeString) =>
            NodeLibDict.ContainsKey(libName) ? NodeLibDict[libName].CreateNode(typeString) : null;

        #endregion

        #region 私有方法

        /// <summary>
        /// 构建内置节点库
        /// </summary>
        private void BuildInnerNodeLib()
        {
            // 创建根文件夹
            Folder 内置节点 = _nodeLibRoot.CreateFolder("内置节点".PackToList());

            // 创建一级文件夹
            Folder 基础节点 = _nodeLibRoot.CreateFolder(内置节点, "基础节点".PackToList());
            Folder 动作节点 = _nodeLibRoot.CreateFolder(内置节点, "动作节点".PackToList());
            Folder 控制流节点 = _nodeLibRoot.CreateFolder(内置节点, "控制流节点".PackToList());
            Folder 图像识别节点 = _nodeLibRoot.CreateFolder(内置节点, "图像识别节点".PackToList());

            // 基础节点
            _nodeLibRoot.CreateFile(基础节点, "开始", "nt", new NodeType<StartNode>());
            _nodeLibRoot.CreateFile(基础节点, "结束", "nt", new NodeType<EndNode>());

            // 动作节点
            _nodeLibRoot.CreateFile(动作节点, "鼠标点击", "nt", new NodeType<MouseClickNode>());
            _nodeLibRoot.CreateFile(动作节点, "延迟", "nt", new NodeType<DelayNode>());

            // 控制流节点
            _nodeLibRoot.CreateFile(控制流节点, "条件判断", "nt", new NodeType<ConditionNode>());
            _nodeLibRoot.CreateFile(控制流节点, "循环", "nt", new NodeType<LoopNode>());
            _nodeLibRoot.CreateFile(控制流节点, "智能循环", "nt", new NodeType<SmartLoopNode>());

            // 图像识别节点
            _nodeLibRoot.CreateFile(图像识别节点, "屏幕找图", "nt", new NodeType<ScreenFindImageNode>());
            _nodeLibRoot.CreateFile(图像识别节点, "区域找图", "nt", new NodeType<RegionFindImageNode>());
            _nodeLibRoot.CreateFile(图像识别节点, "设置区域", "nt", new NodeType<RegionSelectorNode>());
            _nodeLibRoot.CreateFile(图像识别节点, "等待图像", "nt", new NodeType<WaitForImageNode>());
            _nodeLibRoot.CreateFile(图像识别节点, "区域查找与选择", "nt", new NodeType<IntegratedRegionFindNode>());
        }

        /// <summary>
        /// 加载外部节点库
        /// </summary>
        private void LoadOutsideNodeLib()
        {
            // 遍历节点库文件
            foreach (var dllPath in GetAllNodeLibDll())
            {
                // 加载动态库
                Assembly dll = Assembly.LoadFrom(dllPath);
                // 遍历全部类
                foreach (var type in dll.GetTypes())
                {
                    if (typeof(INodeLib).IsAssignableFrom(type))
                    {
                        // 获取单例
                        PropertyInfo? propertyInfo = type.GetProperty("Instance");
                        if (propertyInfo == null) continue;
                        if (propertyInfo.GetValue(null) is not INodeLib instance) continue;
                        // 初始化单例
                        instance.Init();
                        // 保存引用
                        NodeLibDict.Add(instance.Name, instance);
                    }
                }
            }
            // 遍历节点库
            foreach (var libPair in NodeLibDict)
            {
                // 创建根文件夹
                Folder root = _nodeLibRoot.CreateFolder(libPair.Value.Title.PackToList());
                // 加载文件夹
                LoadFolder(root, libPair.Value.LibHarddisk.Root);
            }
        }

        /// <summary>
        /// 获取全部节点库文件
        /// </summary>
        private List<string> GetAllNodeLibDll()
        {
            if (!Directory.Exists(OptionManager.Instance.NodeLibPath)) return new List<string>();

            DirectoryInfo directoryInfo = new DirectoryInfo(OptionManager.Instance.NodeLibPath);
            List<string> result = new List<string>();
            foreach (var fileInfo in directoryInfo.GetFiles())
            {
                if (fileInfo.Extension == ".dll") result.Add(fileInfo.FullName);
            }
            return result;
        }

        /// <summary>
        /// 加载文件夹至目标文件夹
        /// </summary>
        private void LoadFolder(Folder target, Folder oldFolder)
        {
            // 加载文件夹
            foreach (var oldChild in oldFolder.FolderList)
            {
                // 创建子文件夹
                Folder childFolder = new Folder(oldChild.Name, target);
                // 添加子文件夹
                target.FolderList.Add(childFolder);
                // 递归加载
                LoadFolder(childFolder, oldChild);
            }
            // 加载文件
            foreach (var oldFile in oldFolder.FileList)
            {
                // 创建文件
                _nodeLibRoot.CreateFile(target, oldFile.Name, oldFile.Extension, oldFile.Instance);
            }
        }

        #endregion

        #region 字段

        /// <summary>节点库磁盘</summary>
        private readonly Harddisk _nodeLibRoot = new Harddisk();

        #endregion
    }
}