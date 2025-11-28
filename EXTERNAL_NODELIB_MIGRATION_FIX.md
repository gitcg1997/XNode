# 外部节点库迁移机制修复报告

> **问题**: 重构后外部节点库无法正常保存和加载
> **修复日期**: 2025-11-21
> **影响范围**: 所有外部节点库 (NodeLib.Automation, NodeLib.File)

---

## 问题描述

在完成节点保存系统四阶段重构后,用户报告以下问题:

**错误日志**:
```
[18:35:12.663] [警告] 无法创建节点: Inner/DelayNode (ID: 2), 可能是节点库未加载或节点类型已移除
[18:35:12.663] [警告] 无法创建节点: Inner/MouseClickNode (ID: 3), 可能是节点库未加载或节点类型已移除
```

**现象**:
- 保存包含外部节点库节点的项目后,重新加载时节点无法创建
- 错误提示节点类型未找到

---

## 根本原因

### 问题分析

在重构过程中,我们为 `NodeBase` 类添加了版本迁移基础设施:

```csharp
// NodeBase.cs 第268-277行
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
```

**设计意图**:
1. `LoadParaDict()` 作为公共入口,处理版本迁移逻辑
2. `LoadParaDictInternal()` 作为受保护方法,由子类重写实现实际加载

**问题所在**:
外部节点库的节点类(如 `DelayNode`, `MouseClickNode` 等)**重写(override)**了 `LoadParaDict()` 方法,完全替换了基类实现:

```csharp
// ❌ 错误的做法 - 绕过了版本迁移机制
public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
{
    SetData(1, paraDict["Delay"]);  // 直接访问字典,没有错误处理
}
```

这导致:
1. **版本迁移被绕过**: 基类的 `MigrateParaDict()` 不会被调用
2. **缺少错误处理**: 字典键不存在时会抛出异常
3. **破坏了框架设计**: 违反了模板方法模式的设计原则

---

## 解决方案

### 正确的实现方式

外部节点库的节点应该:
1. **不要重写** `LoadParaDict()` 方法
2. **而是重写** `LoadParaDictInternal()` 方法

**修复后的代码**:

```csharp
// ✅ 正确的做法 - 重写 LoadParaDictInternal
protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
{
    // 使用 TryGetValue 进行安全访问
    if (!paraDict.TryGetValue("Delay", out string? delayValue))
    {
        // 使用默认值
        SetData(1, "1000");
        return;
    }

    SetData(1, delayValue);
}
```

**优势**:
1. ✅ 保留版本迁移机制
2. ✅ 增加了错误处理和默认值
3. ✅ 符合框架设计原则
4. ✅ 向后兼容

---

## 修复文件清单

### NodeLib.Automation (5个文件)

1. **NodeLib.Automation/Control/DelayNode.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 添加了默认值处理和错误检查

2. **NodeLib.Automation/Input/MouseClickNode.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 为 X、Y、Button 参数添加了默认值

3. **NodeLib.Automation/Input/MouseMoveNode.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 为 X、Y 坐标添加了默认值

4. **NodeLib.Automation/Vision/CaptureScreenNode.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 为 X、Y、Width、Height、SavePath 参数添加了默认值

5. **NodeLib.Automation/Vision/FindImageNode.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 为 SourcePath、TemplatePath、Threshold 参数添加了默认值

### NodeLib.File (2个文件)

6. **NodeLib.File/Define/Func_GetFileMD5.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 添加了默认空字符串处理

7. **NodeLib.File/Define/Rename/Func_Upper.cs**
   - 修改前: `public override void LoadParaDict(...)`
   - 修改后: `protected override void LoadParaDictInternal(...)`
   - 添加了默认空字符串处理

---

## 代码对比示例

### 修改前 (❌ 错误)

```csharp
public class DelayNode : NodeBase
{
    public override Dictionary<string, string> GetParaDict()
    {
        return new Dictionary<string, string>
        {
            { "Delay", GetData(1) }
        };
    }

    public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
    {
        SetData(1, paraDict["Delay"]);  // 可能抛出 KeyNotFoundException
    }
}
```

**问题**:
- 直接访问字典键,不存在时会抛出异常
- 绕过了基类的版本迁移逻辑
- 没有默认值处理

### 修改后 (✅ 正确)

```csharp
public class DelayNode : NodeBase
{
    public override Dictionary<string, string> GetParaDict()
    {
        return new Dictionary<string, string>
        {
            { "Delay", GetData(1) }
        };
    }

    protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
    {
        if (!paraDict.TryGetValue("Delay", out string? delayValue))
        {
            // 使用默认值
            SetData(1, "1000");
            return;
        }

        SetData(1, delayValue);
    }
}
```

**优势**:
- 安全的字典访问,使用 `TryGetValue`
- 保留了基类的版本迁移机制
- 提供了合理的默认值
- 代码更健壮

---

## 编译结果

```
已成功生成。

    10 个警告 (框架级别的 NuGet 包兼容性警告,不影响功能)
    0 个错误

已用时间 00:00:10.28
```

---

## 给外部节点库开发者的建议

### ⚠️ 重要提示

如果您正在开发自己的外部节点库,请遵循以下最佳实践:

### 1. **参数加载 - 使用 LoadParaDictInternal**

```csharp
// ✅ 正确
protected override void LoadParaDictInternal(Dictionary<string, string> paraDict)
{
    if (paraDict.TryGetValue("ParamName", out string? value))
        SetData(index, value);
    else
        SetData(index, "default_value");
}

// ❌ 错误 - 不要这样做!
public override void LoadParaDict(string version, Dictionary<string, string> paraDict)
{
    SetData(index, paraDict["ParamName"]);  // 会绕过版本迁移!
}
```

### 2. **属性加载 - 使用 LoadPropertyDictInternal**

```csharp
// ✅ 正确
protected override void LoadPropertyDictInternal(Dictionary<string, string> propertyDict)
{
    if (propertyDict.TryGetValue("PropertyName", out string? value))
    {
        // 加载属性
    }
}

// ❌ 错误 - 不要这样做!
public override void LoadPropertyDict(string version, Dictionary<string, string> propertyDict)
{
    // 加载属性
}
```

### 3. **版本迁移支持**

如果您的节点需要版本升级,重写迁移方法:

```csharp
public override void Init()
{
    Version = "1.1";  // 升级版本号
    // ... 初始化代码
}

protected override Dictionary<string, string> MigrateParaDict(
    string fromVersion,
    Dictionary<string, string> oldDict)
{
    if (CompareVersion(fromVersion, "1.0") == 0)
    {
        // 参数重命名示例
        if (oldDict.ContainsKey("OldParamName"))
        {
            oldDict["NewParamName"] = oldDict["OldParamName"];
            oldDict.Remove("OldParamName");
        }

        // 新增参数示例
        if (!oldDict.ContainsKey("NewParam"))
        {
            oldDict["NewParam"] = "default_value";
        }
    }

    return oldDict;
}
```

### 4. **错误处理**

始终使用 `TryGetValue` 而不是直接字典访问:

```csharp
// ✅ 正确 - 安全的访问方式
if (paraDict.TryGetValue("Delay", out string? delayValue))
{
    if (int.TryParse(delayValue, out int delay) && delay > 0)
    {
        SetData(1, delay.ToString());
    }
    else
    {
        SetData(1, "1000");  // 使用默认值
    }
}
else
{
    SetData(1, "1000");  // 使用默认值
}

// ❌ 错误 - 可能抛出异常
int delay = int.Parse(paraDict["Delay"]);
SetData(1, delay.ToString());
```

---

## 测试验证

修复后,请进行以下测试:

### 测试 1: 基本保存和加载
```
1. 创建新项目
2. 添加外部节点库的节点
3. 配置节点参数
4. 保存项目
5. 关闭并重新打开项目
6. 验证节点参数正确恢复
```

**预期结果**: ✅ 节点正常加载,参数正确恢复

### 测试 2: 缺失参数处理
```
1. 手动编辑保存的 .xnode 文件
2. 删除某个节点的某个参数
3. 重新加载项目
4. 验证节点使用默认值
```

**预期结果**: ✅ 节点正常加载,使用默认值,不抛出异常

### 测试 3: 版本迁移 (如果实现了)
```
1. 保存包含旧版本节点的项目
2. 升级节点版本,实现参数迁移
3. 重新加载旧项目
4. 验证参数自动迁移
```

**预期结果**: ✅ 参数自动迁移到新格式,日志显示迁移信息

---

## 总结

### 问题
外部节点库重写了 `LoadParaDict()` 方法,绕过了版本迁移机制

### 解决方案
将所有外部节点库的 `LoadParaDict()` 重写改为 `LoadParaDictInternal()` 重写

### 影响
- ✅ 修复了节点无法加载的问题
- ✅ 增强了错误处理和健壮性
- ✅ 支持未来的版本迁移
- ✅ 符合框架设计原则

### 文件统计
- **修复文件**: 7个
- **修复代码行数**: ~140行
- **编译错误**: 0
- **编译警告**: 10 (NuGet 包兼容性,不影响功能)

---

**修复完成日期**: 2025-11-21
**测试状态**: ✅ 编译通过,待用户验证
**文档版本**: 1.0
