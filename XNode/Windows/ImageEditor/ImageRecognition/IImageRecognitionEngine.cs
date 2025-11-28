using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using XNode.Windows.ImageEditor.Models;

namespace XNode.Windows.ImageEditor.ImageRecognition
{
    public interface IImageRecognitionEngine
    {
        Task<MatchResult?> FindTemplateAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default);
        Task<List<MatchResult>> FindAllTemplatesAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default);
        Task<MatchResult?> FindByFeaturesAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default);
        Task<MatchResult?> FindTemplateInRegionAsync(Bitmap sourceImage, Bitmap templateImage, Rectangle region, ImageRecognitionConfig config, CancellationToken cancellationToken = default);
        Task<MatchResult?> FindByFeatureMatchingAsync(Bitmap sourceImage, Bitmap templateImage, ImageRecognitionConfig config, CancellationToken cancellationToken = default);
        Task<bool> IsImagePresentAsync(Bitmap sourceImage, Bitmap templateImage, double threshold = 0.8, CancellationToken cancellationToken = default);
        Task<double> CalculateSimilarityAsync(Bitmap image1, Bitmap image2);
    }
}
