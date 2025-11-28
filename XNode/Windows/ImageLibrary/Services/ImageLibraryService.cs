using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using XNode.Windows.ImageLibrary.Models;

namespace XNode.Windows.ImageLibrary.Services
{
    /// <summary>
    /// 图片库管理服务
    /// 提供图片和分类的高级管理功能
    /// </summary>
    public class ImageLibraryService : IDisposable
    {
        private readonly ImageLibraryDatabase _database;
        private readonly string _imagesDirectory;
        private readonly string _thumbnailsDirectory;

        public ImageLibraryService()
        {
            _database = new ImageLibraryDatabase();

            // 设置图片存储目录
            var docsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var basePath = Path.Combine(docsPath, "XNode", "ImageLibrary");
            _imagesDirectory = Path.Combine(basePath, "Images");
            _thumbnailsDirectory = Path.Combine(basePath, "Thumbnails");

            Directory.CreateDirectory(_imagesDirectory);
            Directory.CreateDirectory(_thumbnailsDirectory);
        }

        #region 图片管理

        /// <summary>
        /// 添加图片到库
        /// </summary>
        /// <param name="sourceImagePath">源图片路径</param>
        /// <param name="name">图片名称</param>
        /// <param name="categoryName">分类名称</param>
        /// <param name="description">描述</param>
        /// <param name="tags">标签数组</param>
        /// <returns>添加的图片项</returns>
        public ImageItem AddImage(string sourceImagePath, string name, string categoryName = "截图", string? description = null, string[]? tags = null)
        {
            if (!File.Exists(sourceImagePath))
                throw new FileNotFoundException("源图片文件不存在", sourceImagePath);

            // 获取或创建分类
            var categoryId = _database.GetCategoryIdByName(categoryName);
            if (categoryId == 0)
            {
                // 分类不存在,使用"未分类"
                categoryId = _database.GetCategoryIdByName("未分类");
            }

            // 生成文件名(按年月组织)
            var yearMonth = DateTime.Now.ToString("yyyy-MM");
            var targetDir = Path.Combine(_imagesDirectory, yearMonth);
            Directory.CreateDirectory(targetDir);

            var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{Path.GetFileName(sourceImagePath)}";
            var targetPath = Path.Combine(targetDir, fileName);

            // 复制图片文件
            File.Copy(sourceImagePath, targetPath, true);

            // 获取图片信息
            var fileInfo = new FileInfo(targetPath);
            int width = 0, height = 0;
            string format = "PNG";

            using (var img = Image.FromFile(targetPath))
            {
                width = img.Width;
                height = img.Height;
                format = img.RawFormat.ToString().ToUpper();
            }

            // 生成缩略图
            var thumbnailPath = GenerateThumbnail(targetPath, _thumbnailsDirectory);

            // 创建图片项
            var imageItem = new ImageItem
            {
                Name = name,
                Description = description,
                FilePath = targetPath,
                ThumbnailPath = thumbnailPath,
                CategoryId = categoryId,
                Tags = tags != null && tags.Length > 0 ? Newtonsoft.Json.JsonConvert.SerializeObject(tags) : null,
                Width = width,
                Height = height,
                FileSize = fileInfo.Length,
                Format = format,
                UsageCount = 0,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsFavorite = false
            };

            // 添加到数据库
            var id = _database.AddImage(imageItem);
            imageItem.Id = id;

            // 更新分类图片计数
            _database.UpdateAllCategoryImageCounts();

            return imageItem;
        }

        /// <summary>
        /// 生成缩略图
        /// </summary>
        private string GenerateThumbnail(string imagePath, string thumbnailDir)
        {
            const int thumbnailSize = 200;

            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var thumbnailFileName = $"{fileName}_thumb.png";
            var thumbnailPath = Path.Combine(thumbnailDir, thumbnailFileName);

            try
            {
                using var originalImage = Image.FromFile(imagePath);

                int width, height;
                if (originalImage.Width > originalImage.Height)
                {
                    width = thumbnailSize;
                    height = (int)(originalImage.Height * (thumbnailSize / (double)originalImage.Width));
                }
                else
                {
                    height = thumbnailSize;
                    width = (int)(originalImage.Width * (thumbnailSize / (double)originalImage.Height));
                }

                using var thumbnail = new Bitmap(width, height);
                using (var graphics = Graphics.FromImage(thumbnail))
                {
                    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                    graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                    graphics.DrawImage(originalImage, 0, 0, width, height);
                }

                thumbnail.Save(thumbnailPath, ImageFormat.Png);

                return thumbnailPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成缩略图失败: {ex.Message}");
                return imagePath; // 降级使用原图
            }
        }

        /// <summary>
        /// 批量导入图片
        /// </summary>
        public List<ImageItem> BatchImportImages(string[] imagePaths, string categoryName = "截图")
        {
            var results = new List<ImageItem>();

            foreach (var path in imagePaths)
            {
                try
                {
                    var name = Path.GetFileNameWithoutExtension(path);
                    var item = AddImage(path, name, categoryName);
                    results.Add(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导入图片失败 {path}: {ex.Message}");
                }
            }

            return results;
        }

        /// <summary>
        /// 更新图片信息
        /// </summary>
        public void UpdateImage(ImageItem image)
        {
            _database.UpdateImage(image);
            _database.UpdateAllCategoryImageCounts();
        }

        /// <summary>
        /// 删除图片
        /// </summary>
        public void DeleteImage(int id)
        {
            var image = _database.GetAllImages().FirstOrDefault(x => x.Id == id);
            if (image == null) return;

            // 删除文件
            try
            {
                if (File.Exists(image.FilePath))
                    File.Delete(image.FilePath);

                if (!string.IsNullOrEmpty(image.ThumbnailPath) && File.Exists(image.ThumbnailPath))
                    File.Delete(image.ThumbnailPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除图片文件失败: {ex.Message}");
            }

            // 从数据库删除
            _database.DeleteImage(id);
            _database.UpdateAllCategoryImageCounts();
        }

        /// <summary>
        /// 批量删除图片
        /// </summary>
        public void BatchDeleteImages(IEnumerable<int> ids)
        {
            var images = _database.GetAllImages().Where(x => ids.Contains(x.Id)).ToList();

            // 删除文件
            foreach (var image in images)
            {
                try
                {
                    if (File.Exists(image.FilePath))
                        File.Delete(image.FilePath);

                    if (!string.IsNullOrEmpty(image.ThumbnailPath) && File.Exists(image.ThumbnailPath))
                        File.Delete(image.ThumbnailPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"删除图片文件失败: {ex.Message}");
                }
            }

            // 从数据库删除
            _database.DeleteImages(ids);
            _database.UpdateAllCategoryImageCounts();
        }

        /// <summary>
        /// 获取所有图片
        /// </summary>
        public List<ImageItem> GetAllImages()
        {
            return _database.GetAllImages();
        }

        /// <summary>
        /// 根据分类获取图片
        /// </summary>
        public List<ImageItem> GetImagesByCategory(int categoryId)
        {
            // 如果是"全部"分类(ID=1),返回所有图片
            var category = _database.GetAllCategories().FirstOrDefault(c => c.Id == categoryId);
            if (category != null && category.Name == "全部")
            {
                return _database.GetAllImages();
            }

            return _database.GetImagesByCategory(categoryId);
        }

        /// <summary>
        /// 搜索图片
        /// </summary>
        public List<ImageItem> SearchImages(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return GetAllImages();

            return _database.SearchImages(searchTerm);
        }

        /// <summary>
        /// 获取收藏的图片
        /// </summary>
        public List<ImageItem> GetFavoriteImages()
        {
            return _database.GetFavoriteImages();
        }

        /// <summary>
        /// 切换收藏状态
        /// </summary>
        public void ToggleFavorite(int id)
        {
            _database.ToggleFavorite(id);
        }

        /// <summary>
        /// 增加使用次数
        /// </summary>
        public void IncrementUsageCount(int id)
        {
            _database.IncrementUsageCount(id);
        }

        #endregion

        #region 分类管理

        /// <summary>
        /// 获取所有分类
        /// </summary>
        public List<Category> GetAllCategories()
        {
            var categories = _database.GetAllCategories();

            // 更新每个分类的图片数量
            foreach (var category in categories)
            {
                if (category.Name == "全部")
                {
                    category.ImageCount = _database.GetTotalImageCount();
                }
                else
                {
                    category.ImageCount = _database.GetCategoryImageCount(category.Id);
                }
            }

            return categories;
        }

        /// <summary>
        /// 添加分类
        /// </summary>
        public int AddCategory(Category category)
        {
            return _database.AddCategory(category);
        }

        /// <summary>
        /// 更新分类
        /// </summary>
        public void UpdateCategory(Category category)
        {
            _database.UpdateCategory(category);
        }

        /// <summary>
        /// 删除分类
        /// </summary>
        public void DeleteCategory(int id)
        {
            _database.DeleteCategory(id);
            _database.UpdateAllCategoryImageCounts();
        }

        #endregion

        #region 统计信息

        /// <summary>
        /// 获取库统计信息
        /// </summary>
        public (int TotalImages, int TotalCategories, long TotalSize) GetStatistics()
        {
            var totalImages = _database.GetTotalImageCount();
            var totalCategories = _database.GetAllCategories().Count;
            var images = _database.GetAllImages();
            var totalSize = images.Sum(x => x.FileSize);

            return (totalImages, totalCategories, totalSize);
        }

        #endregion

        public void Dispose()
        {
            _database?.Dispose();
        }
    }
}
