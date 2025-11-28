# XNode 版本迁移指南

本文档说明如何使用新的版本迁移机制来保证节点的向后兼容性。

## 概述

从 1.0.3 Alpha 版本开始,XNode 引入了完整的版本迁移系统,允许节点在更新后仍能正确加载旧版本的项目文件。

## 核心架构

### 1. NodeBase 的版本迁移接口

所有节点现在通过以下机制支持版本迁移:

```csharp
// 公开方法 - 自动处理版本迁移
public virtual void LoadParaDict(string version, Dictionary<string, string> paraDict)
{
    // 如果版本不匹配,执行参数迁移
    if (!string.IsNullOrEmpty(version) && version != Version)
    {
        paraDict = MigrateParaDict(version, paraDict);
    }
    // 调用实际加载方法
    LoadParaDictInternal(paraDict);
}

// 子类重写的迁移方法
protected virtual Dictionary<string, string> MigrateParaDict(string fromVersion, Dictionary<string, string> oldDict)
{
    // 默认不做迁移,子类可重写
    return oldDict;
}

// 子类重写的实际加载方法
protected virtual void LoadParaDictInternal(Dictionary<string, string> paraDict)
{
    // 在此实现参数加载逻辑
}
```

### 2. 版本比较工具

NodeBase 提供了 `CompareVersion` 辅助方法:

```csharp
protected int CompareVersion(string version1, string version2)
// 返回值:
//   > 0: version1 更新
//   = 0: 版本相同
//   < 0: version2 更新
```

## 使用指南

### 场景 1: 简单参数验证

对于不需要版本迁移的节点,只需实现参数验证:

```csharp
public class Data_String : NodeBase
{
    public override void Init()
    {
        Version = "1.0";
        // ... 初始化代码
        PinGroupList.Add(new DataPinGroup(this, "string", "数据", "Hello World"));
    }

    public override Dictionary<string, string> GetParaDict()
    {
        return new Dictionary<string, string> { { "Data", GetData(0) } };
    }

    protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
    {
        // 参数验证
        if (!paraDict.TryGetValue("Data", out string? value))
        {
            MainWindow.LogManager.LogWarning($"字符串节点 (ID: {ID}) 缺少参数 'Data', 使用默认值");
            SetData(0, "Hello World");
            return;
        }

        SetData(0, value);
    }
}
```

### 场景 2: 参数重命名

假设你要将 `TimerDriver` 的参数从 "Time" 重命名为 "IntervalMs":

```csharp
public class TimerDriver : NodeBase
{
    public override void Init()
    {
        Version = "1.1";  // 版本升级到 1.1
        // ...
        PinGroupList.Add(new DataPinGroup(this, "double", "间隔毫秒", "5000"));
    }

    public override Dictionary<string, string> GetParaDict()
    {
        return new Dictionary<string, string>
        {
            { "IntervalMs", GetData(0) }  // 新的参数名
        };
    }

    protected override Dictionary<string, string> MigrateParaDict(string fromVersion, Dictionary<string, string> oldDict)
    {
        // 从 1.0 迁移到 1.1
        if (CompareVersion(fromVersion, "1.0") == 0 && Version == "1.1")
        {
            if (oldDict.ContainsKey("Time") && !oldDict.ContainsKey("IntervalMs"))
            {
                MainWindow.LogManager.LogInfo($"定时驱动器 (ID: {ID}) 参数从 'Time' 迁移到 'IntervalMs'");
                oldDict["IntervalMs"] = oldDict["Time"];
                oldDict.Remove("Time");
            }
        }

        return oldDict;
    }

    protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
    {
        // 加载迁移后的参数
        if (!paraDict.TryGetValue("IntervalMs", out string? timeValue))
        {
            MainWindow.LogManager.LogWarning($"定时驱动器 (ID: {ID}) 缺少参数 'IntervalMs', 使用默认值 5000");
            SetData(0, "5000");
            return;
        }

        if (!double.TryParse(timeValue, out double time))
        {
            MainWindow.LogManager.LogWarning($"定时驱动器 (ID: {ID}) 参数格式错误, 使用默认值 5000");
            SetData(0, "5000");
            return;
        }

        SetData(0, time.ToString());
    }
}
```

### 场景 3: 新增参数

假设你要为 `Func_Compare` 添加一个新的 "CaseSensitive" 参数:

```csharp
public class Func_Compare : NodeBase
{
    public override void Init()
    {
        Version = "1.1";  // 版本升级
        // ... 原有引脚组
        // 新增参数可以在属性列表中
    }

    protected override Dictionary<string, string> MigrateParaDict(string fromVersion, Dictionary<string, string> oldDict)
    {
        // 为旧版本添加默认参数
        if (CompareVersion(fromVersion, "1.0") == 0 && Version == "1.1")
        {
            if (!oldDict.ContainsKey("CaseSensitive"))
            {
                MainWindow.LogManager.LogInfo($"比较节点 (ID: {ID}) 添加默认参数 'CaseSensitive' = false");
                oldDict["CaseSensitive"] = "false";
            }
        }

        return oldDict;
    }
}
```

### 场景 4: 复杂的跨版本迁移

如果节点经历了多次版本升级 (1.0 → 1.1 → 1.2):

```csharp
protected override Dictionary<string, string> MigrateParaDict(string fromVersion, Dictionary<string, string> oldDict)
{
    int comparison = CompareVersion(fromVersion, Version);

    if (comparison >= 0)
    {
        // 版本相同或更新,无需迁移
        return oldDict;
    }

    // 按版本依次迁移
    if (CompareVersion(fromVersion, "1.0") == 0)
    {
        // 1.0 → 1.1 的迁移
        oldDict = MigrateFrom_1_0_To_1_1(oldDict);
        fromVersion = "1.1";
    }

    if (CompareVersion(fromVersion, "1.1") == 0)
    {
        // 1.1 → 1.2 的迁移
        oldDict = MigrateFrom_1_1_To_1_2(oldDict);
    }

    return oldDict;
}

private Dictionary<string, string> MigrateFrom_1_0_To_1_1(Dictionary<string, string> oldDict)
{
    // 迁移逻辑
    return oldDict;
}

private Dictionary<string, string> MigrateFrom_1_1_To_1_2(Dictionary<string, string> oldDict)
{
    // 迁移逻辑
    return oldDict;
}
```

## 最佳实践

### 1. 版本号管理

- 使用语义化版本: `主版本.子版本`
- 参数结构变化时增加子版本号
- 节点行为重大变化时增加主版本号

### 2. 日志记录

始终使用 `MainWindow.LogManager` 记录迁移操作:

```csharp
MainWindow.LogManager.LogInfo($"节点 (ID: {ID}) 从版本 {fromVersion} 迁移到 {Version}");
MainWindow.LogManager.LogWarning($"节点 (ID: {ID}) 参数 '{key}' 已弃用, 使用默认值");
```

### 3. 参数验证

在 `LoadParaDictInternal` 中始终验证参数:

- 使用 `TryGetValue` 检查参数是否存在
- 使用 `TryParse` 验证数据类型
- 为所有参数提供合理的默认值
- 记录警告而不是抛出异常

### 4. 测试

迁移代码务必测试:

1. 保存旧版本节点的项目文件
2. 更新节点代码和版本号
3. 加载旧项目文件
4. 验证节点参数正确迁移
5. 检查日志中的迁移信息

### 5. 向后兼容性

- 尽量不删除参数,而是标记为弃用
- 保留旧参数的迁移代码至少一个主版本周期
- 在文档中明确标注弃用的参数

## 错误处理改进

### 增强的 Loader_1_0

新版本的加载器现在提供详细的错误报告:

```
[信息] 开始加载存档: MyProject.xnode
[信息] 成功加载 25 个节点
[警告] 无法创建节点: ExternalLib/OldNode (ID: 15), 可能是节点库未加载或节点类型已移除
[警告] 节点 TimerDriver (ID: 8) 加载参数/属性时出错: ..., 使用默认值
[信息] 成功加载 48 个连接线
[警告] 找不到起始引脚: 节点ID=15, 引脚组=0, 引脚=1
[警告] 共 2 个节点加载失败
[警告] 共 1 个连接线加载失败
[信息] 存档加载完成
```

## 外部节点库

外部节点库开发者也可以使用相同的机制:

```csharp
// NodeLib.File/Define/Func_GetFileMD5.cs
public class Func_GetFileMD5 : NodeBase
{
    public override void Init()
    {
        NodeLibName = "NodeLib.File";  // 重要!指定节点库名称
        Version = "1.0";
        // ...
    }

    protected override Dictionary<string, string> MigrateParaDict(string fromVersion, Dictionary<string, string> oldDict)
    {
        // 实现迁移逻辑
        return oldDict;
    }

    protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
    {
        // 实现参数加载
    }
}
```

## 常见问题

### Q: 如何判断是否需要版本迁移?

A: 如果满足以下任一条件,就需要版本迁移:
- 参数名称改变
- 参数数量改变
- 参数默认值改变(会影响行为)
- 属性结构改变

### Q: 迁移失败会怎样?

A: 迁移失败不会阻止项目加载:
- 节点会使用默认参数值
- 警告会记录到日志中
- 用户可以手动修复节点配置

### Q: 如何处理属性迁移?

A: 与参数迁移相同:
```csharp
protected override Dictionary<string, string> MigratePropertyDict(string fromVersion, Dictionary<string, string> oldDict)
{
    // 迁移逻辑
    return oldDict;
}

protected override void LoadPropertyDictInternal(Dictionary<string, string> propertyDict)
{
    // 加载逻辑
}
```

### Q: 版本比较如何工作?

A: 版本格式为 "主版本.子版本":
- "1.0" < "1.1" < "2.0" < "2.1"
- 比较时先比较主版本,再比较子版本

## 总结

版本迁移机制确保了 XNode 项目的长期可维护性:

✅ **向后兼容**: 旧项目可以在新版本中正常打开
✅ **可追溯**: 详细的日志记录迁移过程
✅ **健壮性**: 迁移失败不会导致项目损坏
✅ **易扩展**: 简单的 API 支持复杂的迁移场景
✅ **统一性**: 内置节点和外部节点使用相同机制

遵循本指南,你可以安全地演化节点定义,而不会破坏用户的现有项目。
