# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

XNode 是一个基于 WPF 的可视化节点编辑器框架,使用 .NET 8 开发。用户可以通过拖拽节点、连接引脚的方式创建可视化工作流程或程序逻辑。

**当前版本**: 1.0.3 Alpha

## 构建和运行

### 构建项目
```bash
dotnet build XNode.sln
```

### 构建 Release 版本
```bash
dotnet build XNode.sln --configuration Release
```

### 运行主应用程序
```bash
dotnet run --project XNode/XNode.csproj
```

### 运行已编译的程序
```bash
cd XNode/bin/Release/net8.0-windows
./XNode.exe
```

## 项目架构

### 核心模块划分

项目采用模块化设计,分为以下库:

- **XNode**: 主应用程序,包含窗口、核心编辑器、工具栏和所有子系统
- **XLib.Node**: 节点核心库,定义节点基类、引脚、节点类型等核心抽象
  - 依赖: XLib.Base
  - 所有节点必须继承 `NodeBase`
- **XLib.Base**: 基础工具库(数据窗口、文件过滤器、高精度计时器、树节点路径等)
  - 无外部依赖,是整个项目的基础层
- **XLib.WPF**: WPF 基础工具库
- **XLib.WPFControl**: WPF 自定义控件库(进度条、工具栏、树视图等)
- **XLib.WPFStyle**: WPF 样式库
- **XLib.Drawing**: 绘图库(像素和位图操作)
- **XLib.Math**: 数学库(范围、缓动函数等)
- **XLib.Animate**: 动画库(动画引擎、延迟、队列等)
- **XLib.Sample**: 示例应用程序
- **NodeLib.File**: 文件节点库示例,演示如何扩展节点库
  - 依赖: XLib.Node
  - 外部节点库的参考实现
- **NodeLib.Automation**: 自动化节点库(新增)

### 子系统架构 (XNode/SubSystem/)

XNode 主应用程序采用子系统架构设计。各子系统通过 Manager 单例模式管理,通过事件系统(`EM`)进行解耦通信。

- **ArchiveSystem**: 存档系统,负责项目的保存和加载
  - `ArchiveManager`: 序列化/反序列化管理器
- **CacheSystem**: 缓存系统,提供应用配置和窗口状态的持久化
  - `CacheManager`: 缓存管理单例
- **ControlSystem**: 控制系统,管理节点执行流程控制
- **EventSystem**: 事件系统核心,系统间解耦通信的基础
  - `EM`: 事件管理器单例,支持事件注册和触发
  - 常用事件: `Project_Changed`, `KeyDown`, `KeyUp`
- **NodeEditSystem**: 节点编辑系统,核心可视化编辑功能
  - `EditPanel`: 节点编辑面板主控件,管理画布交互
  - `Component`: 组件层,职责分离设计
    - `NodeComponent`: 节点管理组件
    - `InteractionComponent`: 用户交互组件
    - `CardComponent`: 卡片渲染组件
    - `DrawingComponent`: 绘图组件
  - `Layer`: 分层渲染系统(网格层、连接线层、悬停框层、选择框层)
  - `Control`: UI 控件(节点视图、引脚组视图、属性面板)
- **NodeLibSystem**: 节点库系统,管理内置和外部节点库
  - `NodeLibManager`: 节点库加载和管理
  - `Define`: 内置节点定义
    - 数据节点 (Data): 存储和传递数据
    - 驱动节点 (Driver): 定时器、帧驱动器等
    - 流程节点 (Flow): 控制执行流程
    - 函数节点 (Function): 功能性操作
    - 事件节点 (Event): 响应系统事件
  - `NodeLibPanel`: 节点库面板 UI
  - 外部节点库路径: `%USERPROFILE%\Documents\XNode\NodeLib\`
- **ProjectSystem**: 项目系统,管理项目生命周期
  - 项目文件扩展名: `.xnode`
  - `NodeProject`: 项目数据模型
  - `ProjectManager`: 项目管理单例,处理新建/打开/保存/另存为
- **ResourceSystem**: 资源系统,统一管理应用资源
  - `ImageResManager`: 图像资源管理器
  - `CursorManager`: 光标资源管理器
  - `PinIconManager`: 引脚图标管理器
- **TimerSystem**: 定时器系统,提供高精度定时功能
- **WindowSystem**: 窗口系统,提供标准化的对话框和消息窗口
  - `WM`: 窗口管理器单例
  - `WM.Main`: 主窗口实例引用

### 核心类说明

**节点系统 (XLib.Node)**
- `NodeBase`: 所有节点的基类 (抽象类)
  - 核心属性:
    - `ID`: 节点唯一标识
    - `Title`: 节点标题
    - `Icon`: 节点图标名称
    - `Point`: 节点在画布上的坐标
    - `Color`: 节点颜色
    - `State`: 节点状态 (启用/禁用)
  - 引脚和属性:
    - `PinGroupList`: 引脚组列表,在节点上显示
    - `PropertyList`: 属性列表,在属性面板中编辑
  - 生命周期方法:
    - `Init()`: 初始化节点,设置引脚组和属性
    - `Load()`: 加载节点,在项目打开时调用
    - `ExecuteNode()`: 执行节点逻辑,核心业务方法
    - `Unload()`: 卸载节点,清理资源
    - `Clear()`: 清空节点状态
  - 进度报告:
    - `OpenProgressBar(IProgressGetter)`: 显示进度条
    - `CloseProgressBar()`: 关闭进度条
- `PinGroupBase`: 引脚组基类,管理输入和输出引脚
- `PinBase`: 引脚基类,表示节点的数据输入/输出端口
- `NodeProperty`: 节点属性,用于在属性面板中显示和编辑配置
- `INodeLib`: 节点库接口,外部节点库必须实现
  - `Init()`: 初始化库
  - `CreateNode(string typeString)`: 工厂方法,根据类型字符串创建节点实例
  - `Clear()`: 清理库资源

**命令系统 (XNode/Command/)**
- `CommandManager`: 撤销/重做管理器,使用双栈实现
  - `ExecuteCommand(ICommand)`: 执行命令并记录到历史
  - `Undo()`: 撤销上一个命令
  - `Redo()`: 重做上一个撤销的命令
  - `CanUndo` / `CanRedo`: 判断是否可以撤销/重做
  - `CommandStatusChanged`: 命令状态变化事件
  - 最大历史记录: 50 条
- `ICommand`: 命令接口,所有可撤销操作必须实现
  - `Execute()`: 执行命令
  - `Undo()`: 撤销命令
  - `Redo()`: 重做命令
  - `Description`: 命令描述
- 内置命令实现:
  - `AddNodeCommand`: 添加节点
  - `DeleteNodeCommand`: 删除节点
  - `MoveNodeCommand`: 移动节点
  - `ConnectPinCommand`: 连接引脚
  - `DisconnectPinCommand`: 断开引脚连接

**核心编辑器**
- `CoreEditer`: 核心编辑器控件,包含节点编辑面板和节点库面板
  - 提供 `CommandManager` 访问撤销/重做功能
- `MainWindow`: 主窗口,继承自 `XMainWindow`
  - 负责初始化所有子系统
  - 管理工具栏和快捷键
  - 提供日志系统 `LogManager`
  - 快捷键: Ctrl+Z(撤销), Ctrl+Y(重做)

### 初始化顺序

在 `MainWindow.XWindowLoaded()` 中的关键初始化顺序 (参考 MainWindow.xaml.cs:57-93):

1. 恢复窗口状态 (`RecoverWindowState()`)
2. 设置窗口管理器实例 (`WM.Main = this`)
3. 初始化日志管理器 (`LogManager.Initialize()`)
4. **加载核心编辑器** (`LoadCoreEditer()`) - 必须先执行
5. **初始化工具栏** (`InitToolBar()`) - 必须在核心编辑器之后
6. 订阅命令状态变化事件 (`CommandManager.CommandStatusChanged`)
7. 订阅系统事件 (`EM.Instance.Add()`)
8. 输出启动完成日志

**关键依赖关系**:
- 工具栏初始化**必须**在核心编辑器初始化之后,因为工具栏需要访问 `Editer.CommandManager`
- 违反此顺序会导致运行时异常: "核心编辑器为空" (MainWindow.xaml.cs:42)

### 设计模式和架构原则

1. **单例模式**: 各子系统的 Manager 类都使用单例模式 (`Instance` 属性)
   - 例: `ProjectManager.Instance`, `CacheManager.Instance`, `EM.Instance`

2. **命令模式**: 所有可撤销操作都实现 `ICommand` 接口
   - 支持撤销/重做功能
   - 命令对象封装操作的执行和回滚逻辑

3. **事件驱动**: 子系统间通过事件系统 (`EM`) 解耦
   - 发布/订阅模式,降低模块间耦合
   - 示例: `EM.Instance.Add(EventType.Project_Changed, UpdateTitle)`

4. **分层架构**: 明确的依赖层次
   - 表现层: XNode (WPF 应用)
   - 业务层: 各子系统
   - 核心层: XLib.Node
   - 基础层: XLib.Base

5. **扩展性设计**: 通过接口支持扩展
   - `INodeLib` 接口允许第三方节点库
   - 运行时动态加载外部 DLL

## 开发节点

### 创建新的内置节点

1. 在 `XNode/SubSystem/NodeLibSystem/Define/` 下对应的分类文件夹创建节点类
   - Data: 数据节点
   - Driver: 驱动节点
   - Flow: 流程控制节点
   - Function: 功能节点
   - Event: 事件节点
2. 继承 `NodeBase` 抽象类
3. 实现核心方法:
   - `Init()`: 初始化节点,定义引脚组和属性
     - 在此方法中添加引脚组到 `PinGroupList`
     - 在此方法中添加属性到 `PropertyList`
   - `ExecuteNode()`: 定义节点执行逻辑,这是节点的核心业务代码
     - 从输入引脚读取数据
     - 执行业务逻辑
     - 将结果写入输出引脚
   - (可选) `Load()`: 项目加载时初始化资源
   - (可选) `Unload()`: 清理节点资源
4. 在节点库系统中注册节点类型
   - 添加到 `NodeLibManager` 的节点类型映射表

### 创建外部节点库

外部节点库是扩展 XNode 功能的推荐方式,无需修改主程序即可添加新节点。

**步骤**:

1. 创建新的类库项目,目标框架为 `net8.0`
   ```bash
   dotnet new classlib -n MyNodeLib -f net8.0
   ```

2. 添加对 `XLib.Node` 项目的引用
   ```xml
   <ProjectReference Include="..\XLib.Node\XLib.Node.csproj" />
   ```

3. 创建库类,实现 `INodeLib` 接口:
   ```csharp
   public class MyNodeLib : INodeLib
   {
       public void Init()
       {
           // 初始化库,可以留空
       }

       public NodeBase CreateNode(string typeString)
       {
           // 根据类型字符串创建对应的节点实例
           return typeString switch
           {
               "MyNode" => new MyNode(),
               _ => null
           };
       }

       public void Clear()
       {
           // 清理库资源
       }
   }
   ```

4. 创建节点类,继承 `NodeBase`
   ```csharp
   public class MyNode : NodeBase
   {
       public override void Init()
       {
           Title = "我的节点";
           Icon = "Node";
           // 添加引脚组和属性
       }

       public override void ExecuteNode()
       {
           // 实现节点逻辑
       }
   }
   ```

5. 编译项目
   ```bash
   dotnet build -c Release
   ```

6. 将生成的 DLL 放入用户文档目录
   - 目标路径: `%USERPROFILE%\Documents\XNode\NodeLib\`
   - 复制: `MyNodeLib.dll`

7. 重启 XNode,应用程序会自动加载外部节点库

**参考示例**: `NodeLib.File` 项目是一个完整的外部节点库实现,包含文件 MD5 计算等节点。

## 已知问题和解决方案

### 循环依赖问题
- **问题**: XLib.Base 项目最初包含了对 XLib.Node 的引用,导致循环依赖
- **解决方案**: 将 Command 相关文件从 XLib.Base 移至 XNode 项目
- **注意**: 保持依赖层次清晰,XLib.Base 是基础层,不应依赖其他项目

### 初始化顺序问题
- **问题**: MainWindow 中工具栏初始化在核心编辑器初始化之前,导致"核心编辑器为空"错误
- **解决方案**: 调整初始化顺序,确保核心编辑器先于工具栏初始化
- **代码位置**: MainWindow.xaml.cs:72-74
- **错误表现**: 运行时异常,访问 `Editer.CommandManager` 时抛出异常

### 调试和日志

**内置日志系统** (MainWindow.LogManager):
- 日志级别: `LogInfo()`, `LogWarning()`, `LogError()`
- 日志格式: `[HH:mm:ss.fff] [级别] 消息`
- UI 显示: 主窗口底部日志输出区域,可通过日志切换按钮显示/隐藏
- 控制台输出: 可通过工具栏"控制台"按钮打开系统控制台

**常用调试点**:
- 节点执行: 在 `NodeBase.ExecuteNode()` 中添加日志
- 命令执行: 在 `ICommand.Execute/Undo/Redo` 中添加日志
- 事件触发: 在 `EM.Instance.Invoke()` 调用点添加日志
- 项目加载: 在 `ArchiveManager` 的序列化/反序列化方法中添加日志

## 技术栈

- **语言**: C#
- **框架**: .NET 8, WPF (Windows Presentation Foundation)
- **依赖**: Newtonsoft.Json 13.0.3
- **平台**: Windows (net8.0-windows)

## 命名约定

- 类、属性和方法: PascalCase
- 局部变量和参数: camelCase
- 命名空间: PascalCase
- 私有字段: _camelCase (下划线前缀)

## 项目文件格式

- 项目文件扩展名: `.xnode`
- 使用 JSON 格式序列化 (Newtonsoft.Json)
- 版本化数据格式 (当前为 Data_1_0)
