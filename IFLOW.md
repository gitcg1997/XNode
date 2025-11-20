# XNode 项目概览

## 项目概述

XNode 是一个基于 WPF (Windows Presentation Foundation) 的可视化节点编辑器框架，使用 .NET 8 作为运行时环境。该框架允许用户通过拖拽节点、连接引脚的方式来创建可视化的工作流程或程序逻辑。项目包含多个库模块，支持节点的创建、编辑、执行和管理，并提供项目管理、资源管理、事件系统等功能。

**版本**: XNode 1.0.3 Alpha

## 项目结构

- **XNode**: 主应用程序，包含窗口、核心编辑器、工具栏和日志系统。
- **XLib.Animate**: 动画库，提供动画引擎、延迟、队列等功能。
- **XLib.Base**: 基础库，包含数据窗口、文件过滤器、高精度计时器、树节点路径等基础功能。
- **XLib.Drawing**: 绘图库，包含像素和位图操作。
- **XLib.Math**: 数学库，提供范围、缓动函数等功能。
- **XLib.Node**: 节点库，定义了节点基类、引脚、节点类型等核心节点系统。
- **XLib.Sample**: 示例应用程序，可能用于演示 XLib 库的使用。
- **XLib.WPF**: WPF 工具库，提供基础的 WPF 功能。
- **XLib.WPFControl**: WPF 控件库，包含进度条、工具栏、树视图等控件。
- **XLib.WPFStyle**: WPF 样式库，提供按钮、滚动条、文本框等控件的样式。
- **NodeLib.File**: 文件节点库，包含文件相关的节点定义。

## 核心功能

### 节点系统
- **NodeBase**: 所有节点的基类，定义了节点的基本属性（ID、坐标、颜色、标题等）、生命周期方法（Init、Load、Execute）、引脚组和属性列表。
- **PinGroupBase**: 引脚组基类，管理输入和输出引脚。
- **NodeProperty**: 节点属性，用于在属性面板中显示和编辑节点参数。

### 编辑器
- **CoreEditer**: 核心编辑器控件，包含节点编辑面板和节点库面板。
- **EditPanel**: 节点编辑面板，负责节点的显示、拖拽、连接等操作。
- **NodeLibPanel**: 节点库面板，显示可用的节点类型供用户选择。

### 命令系统
- **CommandManager**: 命令管理器，支持撤销/重做操作。
- **ICommand**: 命令接口，定义命令的基本操作。
- **AddNodeCommand**: 添加节点命令。
- **DeleteNodeCommand**: 删除节点命令。
- **MoveNodeCommand**: 移动节点命令。
- **ConnectPinCommand**: 连接引脚命令。
- **DisconnectPinCommand**: 断开引脚连接命令。

### 子系统架构
- **ArchiveSystem**: 存档系统，负责项目的保存和加载。
- **CacheSystem**: 缓存系统，提供数据缓存功能。
- **ControlSystem**: 控制系统，管理应用程序的控制流程。
- **EventSystem**: 事件系统，处理应用程序内部事件。
- **NodeEditSystem**: 节点编辑系统，提供节点编辑的核心功能。
- **NodeLibSystem**: 节点库系统，管理内置和外部节点库。
- **OptionSystem**: 选项系统，管理应用程序配置。
- **ProjectSystem**: 项目系统，管理项目的创建、打开、保存等操作。
- **ResourceSystem**: 资源系统，管理图像等资源文件。
- **TimerSystem**: 定时器系统，提供定时功能。
- **WindowSystem**: 窗口系统，提供自定义窗口功能。

### 事件系统
- **EM (Event Manager)**: 事件管理器，用于在系统各部分之间传递事件。

### 资源管理
- **ImageResManager**: 图像资源管理器，用于加载和管理图像资源。
- **PinIconManager**: 引脚图标管理器，管理引脚的图标资源。

### 窗口系统
- **XMainWindow**: 自定义主窗口，提供窗口状态管理等功能。
- **WM**: 窗口管理器，管理窗口状态和行为。

## 技术栈

- **语言**: C# (CSharp)
- **框架**: .NET 8, WPF (Windows Presentation Foundation)
- **UI 控件**: 自定义 WPF 控件（如 ToolBar、TreeItem 等）
- **依赖库**: Newtonsoft.Json 13.0.3

## 构建和运行

### 先决条件
- .NET 8 SDK
- Windows 操作系统（WPF 应用程序）

### 构建
1. 使用命令行构建解决方案：
   ```bash
   dotnet build XNode.sln
   ```

2. 或构建 Release 版本：
   ```bash
   dotnet build XNode.sln --configuration Release
   ```

### 运行
```bash
dotnet run --project XNode/XNode.csproj
```

或直接运行编译后的可执行文件：
```bash
cd XNode\bin\Release\net8.0-windows
XNode.exe
```

## 项目特点

1. **模块化设计**: 项目被拆分为多个库（XLib.*），每个库负责特定的功能，便于维护和扩展。
2. **可视化编辑**: 通过节点和引脚的连接实现可视化编程。
3. **可扩展性**: 通过节点库系统，可以轻松添加新的节点类型。
4. **完整的生命周期管理**: 节点具有完整的生命周期方法，支持初始化、加载、执行、卸载等阶段。
5. **命令模式**: 实现了完整的撤销/重做功能，支持操作历史管理。
6. **日志系统**: 内置日志系统，便于调试和问题追踪。
7. **子系统架构**: 采用清晰的子系统架构，各系统职责分明，便于理解和维护。

## 已知问题和解决方案

### 循环依赖问题
- **问题**: XLib.Base 项目最初包含了对 XLib.Node 的引用，导致循环依赖。
- **解决方案**: 将 Command 相关文件移至 XNode 项目，并从 XLib.Base 项目中排除 Command 文件夹。

### 初始化顺序问题
- **问题**: MainWindow 中工具栏初始化在核心编辑器初始化之前，导致"核心编辑器为空"错误。
- **解决方案**: 调整初始化顺序，确保核心编辑器先于工具栏初始化。

## 开发约定

1. **命名约定**: 
   - 使用 PascalCase 命名类、属性和方法
   - 使用 camelCase 命名局部变量和参数
   - 命名空间使用 PascalCase 格式

2. **事件处理**: 使用 .NET 标准的事件模式。

3. **资源管理**: 
   - 图像等资源文件通过 Resource 标签在项目文件中定义
   - 使用 ResourceManager 统一管理资源

4. **UI 样式**: 
   - 使用 XAML 样式文件统一 UI 外观
   - 遵循 WPF 最佳实践

5. **代码组织**: 
   - 按功能模块组织代码
   - 使用清晰的命名空间结构
   - 保持接口和实现的分离

## 节点开发

要创建新的节点类型，需要：
1. 继承 `NodeBase` 类
2. 实现 `Init()` 方法来初始化节点
3. 实现 `ExecuteNode()` 方法来定义节点的执行逻辑
4. 定义引脚组和属性
5. 在节点库中注册该节点类型

## 扩展节点库

要创建外部节点库：
1. 创建新的类库项目
2. 实现 `INodeLib` 接口
3. 定义节点类型并继承 `NodeBase`
4. 将编译后的 DLL 放入 NodeLib 目录
5. 应用程序会自动加载外部节点库