# 图片库系统设计文档

## 项目概述

为 XNode 项目重新设计了一个现代化、功能完善的图片库系统,采用 SQLite 进行数据持久化,支持分类管理、标签系统、搜索过滤等高级功能。

## 已完成的核心功能

### 1. 数据模型层

#### ImageItem.cs (`XNode/Windows/ImageLibrary/Models/ImageItem.cs`)
完整的图片项模型,包含:
- 基础信息: ID、名称、描述、文件路径、缩略图
- 分类和标签: CategoryId、CategoryName、Tags (JSON)
- 元数据: 宽度、高度、文件大小、格式
- 统计信息: 使用次数、最后使用时间
- 状态: 是否收藏、备注
- 格式化属性: FileSizeFormatted、Resolution、RelativeTime

#### Category.cs (`XNode/Windows/ImageLibrary/Models/Category.cs`)
分类模型,包含:
- 基础信息: ID、名称、描述
- 显示属性: 图标、颜色
- 统计: 图片数量
- 排序: SortOrder
- 标识: IsSystem (系统分类不可删除)

### 2. 数据库层 (SQLite)

#### ImageLibraryDatabase.cs (`XNode/Windows/ImageLibrary/Services/ImageLibraryDatabase.cs`)

**核心特性:**
- ✅ SQLite 数据库,数据持久化存储
- ✅ 位置: `%USERPROFILE%\Documents\XNode\ImageLibrary\ImageLibrary.db`
- ✅ 完整的表结构设计 (Categories 和 Images 表)
- ✅ 外键约束,级联删除
- ✅ 索引优化 (CategoryId, CreatedAt, IsFavorite, UsageCount)

**系统预置分类:**
- 全部 (ID=1, 显示所有图片)
- 截图 (绿色)
- 图标 (橙色)
- UI元素 (紫色)
- 未分类 (灰色,默认分类)

**数据库操作:**

分类管理:
- `GetAllCategories()` - 获取所有分类
- `AddCategory()` - 添加自定义分类
- `UpdateCategory()` - 更新分类信息
- `DeleteCategory()` - 删除分类 (系统分类受保护)
- `GetCategoryIdByName()` - 根据名称查找分类

图片管理:
- `AddImage()` - 添加图片到库
- `UpdateImage()` - 更新图片信息
- `DeleteImage()` / `DeleteImages()` - 删除图片 (单个/批量)
- `GetAllImages()` - 获取所有图片
- `GetImagesByCategory()` - 按分类筛选
- `SearchImages()` - 搜索图片 (名称/描述/标签)
- `GetFavoriteImages()` - 获取收藏图片
- `IncrementUsageCount()` - 增加使用次数
- `ToggleFavorite()` - 切换收藏状态

统计功能:
- `GetTotalImageCount()` - 图片总数
- `GetCategoryImageCount()` - 分类图片数
- `UpdateAllCategoryImageCounts()` - 更新所有分类计数

### 3. 业务逻辑层

#### ImageLibraryService.cs (`XNode/Windows/ImageLibrary/Services/ImageLibraryService.cs`)

**高级功能:**

图片管理:
- ✅ 智能文件组织 (按年月存储: `Images/2025-01/`)
- ✅ 自动生成高质量缩略图 (200px, 高质量双三次插值)
- ✅ 批量导入图片
- ✅ 批量删除 (自动清理文件)
- ✅ 完整的元数据提取 (宽度、高度、格式、大小)

搜索和过滤:
- ✅ 全文搜索 (名称/描述/标签)
- ✅ 分类过滤
- ✅ 收藏筛选
- ✅ "全部"分类自动聚合

文件管理:
- ✅ 图片存储: `%USERPROFILE%\Documents\XNode\ImageLibrary\Images\`
- ✅ 缩略图: `%USERPROFILE%\Documents\XNode\ImageLibrary\Thumbnails\`
- ✅ 自动创建目录结构
- ✅ 删除时同步清理文件

统计信息:
- ✅ `GetStatistics()` - 获取库统计 (总图片数、分类数、总大小)

## 项目文件结构

```
XNode/
├── Windows/
│   └── ImageLibrary/
│       ├── Models/
│       │   ├── ImageItem.cs        ✅ 已完成
│       │   └── Category.cs         ✅ 已完成
│       ├── Services/
│       │   ├── ImageLibraryDatabase.cs    ✅ 已完成
│       │   └── ImageLibraryService.cs     ✅ 已完成
│       ├── ViewModels/
│       │   └── (待实现 - UI ViewModel)
│       ├── Views/
│       │   └── (待实现 - XAML UI)
│       └── (辅助类)
└── XNode.csproj                     ✅ 已添加 System.Data.SQLite 依赖
```

## 依赖项

已添加到 `XNode.csproj`:
```xml
<PackageReference Include="System.Data.SQLite.Core" Version="1.0.119" />
```

## 使用示例

### 基本用法

```csharp
// 1. 创建服务实例
using var imageLibrary = new ImageLibraryService();

// 2. 添加图片
var item = imageLibrary.AddImage(
    sourceImagePath: @"C:\temp\screenshot.png",
    name: "登录按钮",
    categoryName: "UI元素",
    description: "用户登录界面的登录按钮",
    tags: new[] { "按钮", "登录", "UI" }
);

// 3. 搜索图片
var results = imageLibrary.SearchImages("登录");

// 4. 获取分类的图片
var categories = imageLibrary.GetAllCategories();
var uiImages = imageLibrary.GetImagesByCategory(categories.First(c => c.Name == "UI元素").Id);

// 5. 标记收藏
imageLibrary.ToggleFavorite(item.Id);

// 6. 增加使用次数
imageLibrary.IncrementUsageCount(item.Id);

// 7. 批量操作
var paths = new[] { "image1.png", "image2.png", "image3.png" };
var imported = imageLibrary.BatchImportImages(paths, "截图");

// 8. 删除图片
imageLibrary.DeleteImage(item.Id);
```

### 高级用法

```csharp
// 自定义分类管理
var newCategory = new Category
{
    Name = "自定义分类",
    Description = "我的自定义分类",
    Icon = "Folder",
    Color = "#FF5722",
    SortOrder = 100
};
var categoryId = imageLibrary.AddCategory(newCategory);

// 获取统计信息
var (totalImages, totalCategories, totalSize) = imageLibrary.GetStatistics();
Console.WriteLine($"库中共有 {totalImages} 张图片, {totalCategories} 个分类, 总大小 {totalSize / 1024.0 / 1024.0:F2} MB");

// 收藏图片
var favorites = imageLibrary.GetFavoriteImages();

// 更新图片信息
var image = imageLibrary.GetAllImages().First();
image.Name = "新名称";
image.Description = "新描述";
image.IsFavorite = true;
image.Notes = "这是一个备注";
imageLibrary.UpdateImage(image);
```

## 数据库架构

### Categories 表
```sql
CREATE TABLE Categories (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL UNIQUE,
    Description TEXT,
    Icon TEXT,
    Color TEXT DEFAULT '#1976D2',
    ImageCount INTEGER DEFAULT 0,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    SortOrder INTEGER DEFAULT 0,
    IsSystem INTEGER DEFAULT 0
)
```

### Images 表
```sql
CREATE TABLE Images (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Description TEXT,
    FilePath TEXT NOT NULL,
    ThumbnailPath TEXT,
    CategoryId INTEGER NOT NULL,
    Tags TEXT,
    Width INTEGER DEFAULT 0,
    Height INTEGER DEFAULT 0,
    FileSize INTEGER DEFAULT 0,
    Format TEXT DEFAULT 'PNG',
    UsageCount INTEGER DEFAULT 0,
    LastUsedAt TEXT,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL,
    IsFavorite INTEGER DEFAULT 0,
    Notes TEXT,
    FOREIGN KEY (CategoryId) REFERENCES Categories(Id) ON DELETE CASCADE
)
```

## 下一步工作 (待实现)

### UI 层
1. **ViewModel 层 (MVVM)**:
   - `ImageLibraryViewModel` - 主窗口 ViewModel
   - `CategoryViewModel` - 分类视图模型
   - `ImageItemViewModel` - 图片项视图模型
   - 使用 `CommunityToolkit.Mvvm` 实现数据绑定

2. **XAML 视图**:
   - `ImageLibraryWindow.xaml` - 主窗口
     - 左侧: 分类导航
     - 中间: 图片网格/列表视图
     - 右侧: 详情/预览面板
   - `ImageDetailPanel.xaml` - 图片详情面板
   - `CategoryManageDialog.xaml` - 分类管理对话框

3. **功能增强**:
   - 拖拽导入
   - 多选批量操作
   - 右键菜单
   - 图片预览
   - 导出功能
   - 标签编辑器

### 集成到主窗口
- 更新 `MainWindow.xaml.cs` 中的 `OpenImageLibrary` 事件处理
- 替换现有的 `MultiImageSelectorDialog`

## 构建状态

✅ **项目构建成功!**

```bash
dotnet build XNode.sln --configuration Debug
```

只有少量警告 (nullable 相关),无错误。

## 技术亮点

1. **SQLite 数据库**: 轻量级、无需配置、跨平台
2. **索引优化**: 针对常用查询建立索引
3. **级联删除**: 删除分类时自动将图片移至"未分类"
4. **高质量缩略图**: 使用高质量插值算法
5. **智能文件组织**: 按时间组织,避免单目录文件过多
6. **扩展性设计**: 易于添加新功能 (标签系统、排序、过滤器)

## 注意事项

1. **数据库位置**: `%USERPROFILE%\Documents\XNode\ImageLibrary\ImageLibrary.db`
2. **图片存储**: `%USERPROFILE%\Documents\XNode\ImageLibrary\Images\YYYY-MM\`
3. **缩略图**: `%USERPROFILE%\Documents\XNode\ImageLibrary\Thumbnails\`
4. **系统分类**: 不可删除,但可以编辑
5. **线程安全**: 当前实现未考虑多线程,如需并发访问需要添加锁

## 总结

已完成图片库系统的核心架构和数据层实现,包括:
- ✅ 完整的数据模型
- ✅ SQLite 数据库持久化
- ✅ 高级业务逻辑服务
- ✅ 文件管理和缩略图生成
- ✅ 搜索、过滤、分类管理
- ✅ 统计功能

剩余工作主要是 UI 层的实现 (ViewModel + XAML),核心功能已经完备且可用。
