using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using XNode.Windows.ImageLibrary.Models;
using Newtonsoft.Json;

namespace XNode.Windows.ImageLibrary.Services
{
    /// <summary>
    /// 图片库数据库服务
    /// 使用 SQLite 进行数据持久化
    /// </summary>
    public class ImageLibraryDatabase : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _databasePath;
        private SQLiteConnection? _connection;

        public ImageLibraryDatabase()
        {
            // 数据库文件保存在用户文档目录
            var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var appDataPath = Path.Combine(docsPath, "XNode", "ImageLibrary");
            Directory.CreateDirectory(appDataPath);

            _databasePath = Path.Combine(appDataPath, "ImageLibrary.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";

            InitializeDatabase();
        }

        /// <summary>
        /// 初始化数据库,创建表结构
        /// </summary>
        private void InitializeDatabase()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 创建分类表
            var createCategoriesTable = @"
                CREATE TABLE IF NOT EXISTS Categories (
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
                )";

            // 创建图片表
            var createImagesTable = @"
                CREATE TABLE IF NOT EXISTS Images (
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
                )";

            // 创建索引
            var createIndexes = @"
                CREATE INDEX IF NOT EXISTS idx_images_category ON Images(CategoryId);
                CREATE INDEX IF NOT EXISTS idx_images_created ON Images(CreatedAt DESC);
                CREATE INDEX IF NOT EXISTS idx_images_favorite ON Images(IsFavorite);
                CREATE INDEX IF NOT EXISTS idx_images_usage ON Images(UsageCount DESC);
            ";

            using (var command = new SQLiteCommand(createCategoriesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand(createImagesTable, connection))
            {
                command.ExecuteNonQuery();
            }

            using (var command = new SQLiteCommand(createIndexes, connection))
            {
                command.ExecuteNonQuery();
            }

            // 初始化系统分类
            InitializeSystemCategories(connection);
        }

        /// <summary>
        /// 初始化系统分类
        /// </summary>
        private void InitializeSystemCategories(SQLiteConnection connection)
        {
            var systemCategories = new[]
            {
                new { Name = "全部", Description = "所有图片", Icon = "ImageMultiple", Color = "#1976D2", SortOrder = 0 },
                new { Name = "截图", Description = "截图图片", Icon = "Monitor", Color = "#388E3C", SortOrder = 1 },
                new { Name = "图标", Description = "图标素材", Icon = "StarCircle", Color = "#F57C00", SortOrder = 2 },
                new { Name = "UI元素", Description = "UI界面元素", Icon = "ViewDashboard", Color = "#7B1FA2", SortOrder = 3 },
                new { Name = "未分类", Description = "未分类图片", Icon = "FolderQuestion", Color = "#616161", SortOrder = 999 }
            };

            foreach (var cat in systemCategories)
            {
                var sql = @"
                    INSERT OR IGNORE INTO Categories (Name, Description, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                    VALUES (@Name, @Description, @Icon, @Color, @SortOrder, 1, @Now, @Now)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@Name", cat.Name);
                command.Parameters.AddWithValue("@Description", cat.Description);
                command.Parameters.AddWithValue("@Icon", cat.Icon);
                command.Parameters.AddWithValue("@Color", cat.Color);
                command.Parameters.AddWithValue("@SortOrder", cat.SortOrder);
                command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("O"));
                command.ExecuteNonQuery();
            }
        }

        #region 分类管理

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public List<Category> GetAllCategories()
        {
            var categories = new List<Category>();

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT * FROM Categories ORDER BY SortOrder, Id";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                categories.Add(ReadCategory(reader));
            }

            return categories;
        }

        /// <summary>
        /// 添加分类
        /// </summary>
        public int AddCategory(Category category)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO Categories (Name, Description, Icon, Color, SortOrder, IsSystem, CreatedAt, UpdatedAt)
                VALUES (@Name, @Description, @Icon, @Color, @SortOrder, @IsSystem, @Now, @Now);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Icon", category.Icon ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Color", category.Color);
            command.Parameters.AddWithValue("@SortOrder", category.SortOrder);
            command.Parameters.AddWithValue("@IsSystem", category.IsSystem ? 1 : 0);
            command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("O"));

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// 更新分类
        /// </summary>
        public void UpdateCategory(Category category)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Categories
                SET Name = @Name, Description = @Description, Icon = @Icon,
                    Color = @Color, SortOrder = @SortOrder, UpdatedAt = @Now
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", category.Id);
            command.Parameters.AddWithValue("@Name", category.Name);
            command.Parameters.AddWithValue("@Description", category.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Icon", category.Icon ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Color", category.Color);
            command.Parameters.AddWithValue("@SortOrder", category.SortOrder);
            command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("O"));

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除分类
        /// </summary>
        public void DeleteCategory(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            // 获取"未分类"的ID
            var uncategorizedId = GetCategoryIdByName("未分类");

            // 将该分类下的图片移动到"未分类"
            var updateSql = "UPDATE Images SET CategoryId = @UncategorizedId WHERE CategoryId = @Id";
            using (var command = new SQLiteCommand(updateSql, connection))
            {
                command.Parameters.AddWithValue("@UncategorizedId", uncategorizedId);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }

            // 删除分类
            var deleteSql = "DELETE FROM Categories WHERE Id = @Id AND IsSystem = 0";
            using (var command = new SQLiteCommand(deleteSql, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// 根据名称获取分类ID
        /// </summary>
        public int GetCategoryIdByName(string name)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT Id FROM Categories WHERE Name = @Name LIMIT 1";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Name", name);

            var result = command.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        #endregion

        #region 图片管理

        /// <summary>
        /// 添加图片
        /// </summary>
        public int AddImage(ImageItem image)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                INSERT INTO Images (Name, Description, FilePath, ThumbnailPath, CategoryId, Tags,
                                   Width, Height, FileSize, Format, UsageCount, LastUsedAt,
                                   CreatedAt, UpdatedAt, IsFavorite, Notes)
                VALUES (@Name, @Description, @FilePath, @ThumbnailPath, @CategoryId, @Tags,
                        @Width, @Height, @FileSize, @Format, @UsageCount, @LastUsedAt,
                        @CreatedAt, @UpdatedAt, @IsFavorite, @Notes);
                SELECT last_insert_rowid();";

            using var command = new SQLiteCommand(sql, connection);
            SetImageParameters(command, image);

            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// 更新图片
        /// </summary>
        public void UpdateImage(ImageItem image)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Images
                SET Name = @Name, Description = @Description, CategoryId = @CategoryId, Tags = @Tags,
                    UpdatedAt = @UpdatedAt, IsFavorite = @IsFavorite, Notes = @Notes
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", image.Id);
            command.Parameters.AddWithValue("@Name", image.Name);
            command.Parameters.AddWithValue("@Description", image.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", image.CategoryId);
            command.Parameters.AddWithValue("@Tags", image.Tags ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@UpdatedAt", DateTime.Now.ToString("O"));
            command.Parameters.AddWithValue("@IsFavorite", image.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("@Notes", image.Notes ?? (object)DBNull.Value);

            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        public void DeleteImage(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "DELETE FROM Images WHERE Id = @Id";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 批量删除图片
        /// </summary>
        public void DeleteImages(IEnumerable<int> ids)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var idList = string.Join(",", ids);
            var sql = $"DELETE FROM Images WHERE Id IN ({idList})";
            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 获取所有图片
        /// </summary>
        public List<ImageItem> GetAllImages()
        {
            return GetImagesByCriteria(null, null, null);
        }

        /// <summary>
        /// 根据分类获取图片
        /// </summary>
        public List<ImageItem> GetImagesByCategory(int categoryId)
        {
            return GetImagesByCriteria($"i.CategoryId = {categoryId}", null, null);
        }

        /// <summary>
        /// 搜索图片
        /// </summary>
        public List<ImageItem> SearchImages(string searchTerm)
        {
            var condition = $"(i.Name LIKE '%{searchTerm}%' OR i.Description LIKE '%{searchTerm}%' OR i.Tags LIKE '%{searchTerm}%')";
            return GetImagesByCriteria(condition, null, null);
        }

        /// <summary>
        /// 获取收藏的图片
        /// </summary>
        public List<ImageItem> GetFavoriteImages()
        {
            return GetImagesByCriteria("i.IsFavorite = 1", null, null);
        }

        /// <summary>
        /// 根据条件获取图片
        /// </summary>
        private List<ImageItem> GetImagesByCriteria(string? whereClause, string? orderBy, int? limit)
        {
            var images = new List<ImageItem>();

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                SELECT i.Id, i.Name, i.Description, i.FilePath, i.ThumbnailPath, i.CategoryId,
                       i.Tags, i.Width, i.Height, i.FileSize, i.Format, i.UsageCount,
                       i.LastUsedAt, i.CreatedAt, i.UpdatedAt, i.IsFavorite, i.Notes,
                       c.Name as CategoryName
                FROM Images i
                LEFT JOIN Categories c ON i.CategoryId = c.Id";

            if (!string.IsNullOrEmpty(whereClause))
                sql += $" WHERE {whereClause}";

            sql += $" ORDER BY {orderBy ?? "i.CreatedAt DESC"}";

            if (limit.HasValue)
                sql += $" LIMIT {limit}";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                images.Add(ReadImageItem(reader));
            }

            return images;
        }

        /// <summary>
        /// 增加图片使用次数
        /// </summary>
        public void IncrementUsageCount(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Images
                SET UsageCount = UsageCount + 1, LastUsedAt = @Now
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        public void ToggleFavorite(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Images
                SET IsFavorite = CASE WHEN IsFavorite = 1 THEN 0 ELSE 1 END,
                    UpdatedAt = @Now
                WHERE Id = @Id";

            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@Now", DateTime.Now.ToString("O"));
            command.ExecuteNonQuery();
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取图片总数
        /// </summary>
        public int GetTotalImageCount()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT COUNT(*) FROM Images";
            using var command = new SQLiteCommand(sql, connection);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// 获取分类的图片数量
        /// </summary>
        public int GetCategoryImageCount(int categoryId)
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = "SELECT COUNT(*) FROM Images WHERE CategoryId = @CategoryId";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@CategoryId", categoryId);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        /// <summary>
        /// 更新所有分类的图片计数
        /// </summary>
        public void UpdateAllCategoryImageCounts()
        {
            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();

            var sql = @"
                UPDATE Categories
                SET ImageCount = (
                    SELECT COUNT(*) FROM Images WHERE Images.CategoryId = Categories.Id
                )";

            using var command = new SQLiteCommand(sql, connection);
            command.ExecuteNonQuery();
        }

        #endregion

        #region 辅助方法

        private void SetImageParameters(SQLiteCommand command, ImageItem image)
        {
            command.Parameters.AddWithValue("@Name", image.Name);
            command.Parameters.AddWithValue("@Description", image.Description ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@FilePath", image.FilePath);
            command.Parameters.AddWithValue("@ThumbnailPath", image.ThumbnailPath ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CategoryId", image.CategoryId);
            command.Parameters.AddWithValue("@Tags", image.Tags ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Width", image.Width);
            command.Parameters.AddWithValue("@Height", image.Height);
            command.Parameters.AddWithValue("@FileSize", image.FileSize);
            command.Parameters.AddWithValue("@Format", image.Format);
            command.Parameters.AddWithValue("@UsageCount", image.UsageCount);
            command.Parameters.AddWithValue("@LastUsedAt", image.LastUsedAt?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@CreatedAt", image.CreatedAt.ToString("O"));
            command.Parameters.AddWithValue("@UpdatedAt", image.UpdatedAt.ToString("O"));
            command.Parameters.AddWithValue("@IsFavorite", image.IsFavorite ? 1 : 0);
            command.Parameters.AddWithValue("@Notes", image.Notes ?? (object)DBNull.Value);
        }

        private Category ReadCategory(SQLiteDataReader reader)
        {
            return new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                Icon = reader.IsDBNull(reader.GetOrdinal("Icon")) ? null : reader.GetString(reader.GetOrdinal("Icon")),
                Color = reader.GetString(reader.GetOrdinal("Color")),
                ImageCount = reader.GetInt32(reader.GetOrdinal("ImageCount")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt"))),
                SortOrder = reader.GetInt32(reader.GetOrdinal("SortOrder")),
                IsSystem = reader.GetInt32(reader.GetOrdinal("IsSystem")) == 1
            };
        }

        private ImageItem ReadImageItem(SQLiteDataReader reader)
        {
            return new ImageItem
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                FilePath = reader.GetString(reader.GetOrdinal("FilePath")),
                ThumbnailPath = reader.IsDBNull(reader.GetOrdinal("ThumbnailPath")) ? null : reader.GetString(reader.GetOrdinal("ThumbnailPath")),
                CategoryId = reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                Tags = reader.IsDBNull(reader.GetOrdinal("Tags")) ? null : reader.GetString(reader.GetOrdinal("Tags")),
                Width = reader.GetInt32(reader.GetOrdinal("Width")),
                Height = reader.GetInt32(reader.GetOrdinal("Height")),
                FileSize = reader.GetInt64(reader.GetOrdinal("FileSize")),
                Format = reader.GetString(reader.GetOrdinal("Format")),
                UsageCount = reader.GetInt32(reader.GetOrdinal("UsageCount")),
                LastUsedAt = reader.IsDBNull(reader.GetOrdinal("LastUsedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("LastUsedAt"))),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt"))),
                IsFavorite = reader.GetInt32(reader.GetOrdinal("IsFavorite")) == 1,
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes")) ? null : reader.GetString(reader.GetOrdinal("Notes"))
            };
        }

        #endregion

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
