using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using XNode.Windows.ImageEditor.Models;

namespace XNode.Windows.ImageEditor.Services
{
    /// <summary>
    /// 图像库服务类 - 管理图像的存储、检索和操作
    /// 简化版本:使用内存存储,未来可扩展为SQLite数据库
    /// </summary>
    public class ImageLibraryService
    {
        private readonly List<ImageLibraryItem> _images = new List<ImageLibraryItem>();
        private readonly string _imagesDirectory;
        private readonly string _thumbnailsDirectory;
        private int _nextId = 1;

        public ImageLibraryService()
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _imagesDirectory = Path.Combine(baseDir, "ImageLibrary");
            _thumbnailsDirectory = Path.Combine(_imagesDirectory, "Thumbnails");

            Directory.CreateDirectory(_imagesDirectory);
            Directory.CreateDirectory(_thumbnailsDirectory);
        }

        /// <summary>
        /// 添加图像到库
        /// </summary>
        public ImageLibraryItem AddImage(string sourceImagePath, string name, string? description = null, string category = "未分类", string[]? tags = null)
        {
            if (!File.Exists(sourceImagePath))
                throw new FileNotFoundException("源图像文件不存在", sourceImagePath);

            // 生成唯一文件名
            var fileName = $"{DateTime.Now:yyyyMMddHHmmssfff}_{Path.GetFileName(sourceImagePath)}";
            var yearMonth = DateTime.Now.ToString("yyyy-MM");
            var targetDir = Path.Combine(_imagesDirectory, yearMonth);
            Directory.CreateDirectory(targetDir);

            var targetPath = Path.Combine(targetDir, fileName);

            // 复制图像文件
            File.Copy(sourceImagePath, targetPath, true);

            // 获取图像信息
            var fileInfo = new FileInfo(targetPath);
            int width = 0, height = 0;

            using (var img = Image.FromFile(targetPath))
            {
                width = img.Width;
                height = img.Height;
            }

            // 生成缩略图
            var thumbnailPath = GenerateThumbnail(targetPath);

            // 创建图像项
            var item = new ImageLibraryItem
            {
                Id = _nextId++,
                Name = name,
                Description = description,
                FilePath = targetPath,
                ThumbnailPath = thumbnailPath,
                Width = width,
                Height = height,
                FileSize = fileInfo.Length,
                Category = category,
                Tags = tags != null ? Newtonsoft.Json.JsonConvert.SerializeObject(tags) : null,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _images.Add(item);
            return item;
        }

        /// <summary>
        /// 生成缩略图
        /// </summary>
        private string GenerateThumbnail(string imagePath)
        {
            const int thumbnailSize = 200;

            var fileName = Path.GetFileNameWithoutExtension(imagePath);
            var thumbnailFileName = $"{fileName}_thumb.png";
            var thumbnailPath = Path.Combine(_thumbnailsDirectory, thumbnailFileName);

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
                graphics.DrawImage(originalImage, 0, 0, width, height);
            }

            thumbnail.Save(thumbnailPath, ImageFormat.Png);

            return thumbnailPath;
        }

        /// <summary>
        /// 获取所有图像
        /// </summary>
        public List<ImageLibraryItem> GetAllImages()
        {
            return _images.OrderByDescending(x => x.CreatedAt).ToList();
        }

        /// <summary>
        /// 搜索图像
        /// </summary>
        public List<ImageLibraryItem> SearchImages(string searchTerm)
        {
            return _images
                .Where(x => x.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (x.Description != null && x.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                           (x.Tags != null && x.Tags.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// 根据分类获取图像
        /// </summary>
        public List<ImageLibraryItem> GetImagesByCategory(string category)
        {
            return _images
                .Where(x => x.Category == category)
                .OrderByDescending(x => x.CreatedAt)
                .ToList();
        }

        /// <summary>
        /// 删除图像
        /// </summary>
        public bool DeleteImage(int id)
        {
            var image = _images.FirstOrDefault(x => x.Id == id);
            if (image == null)
                return false;

            // 删除文件
            try
            {
                if (File.Exists(image.FilePath))
                    File.Delete(image.FilePath);

                if (!string.IsNullOrEmpty(image.ThumbnailPath) && File.Exists(image.ThumbnailPath))
                    File.Delete(image.ThumbnailPath);
            }
            catch (Exception)
            {
                // 忽略文件删除错误
            }

            _images.Remove(image);
            return true;
        }

        /// <summary>
        /// 获取图像总数
        /// </summary>
        public int GetTotalCount()
        {
            return _images.Count;
        }
    }
}
