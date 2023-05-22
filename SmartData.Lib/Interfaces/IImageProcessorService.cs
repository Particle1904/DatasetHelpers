using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Models;

namespace SmartData.Lib.Interfaces
{
    public interface IImageProcessorService
    {
        public Task<System.Drawing.Size> GetImageSizeAsync(string filePath);
        public Task CropImageAsync(string inputPath, string outputPath, List<DetectedPerson> results, float expansionPercentage = 1.1f);
        public Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension);
        public Task ResizeImagesAsync(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension);
        public Task<WDInputData> ProcessImageForTagPredictionAsync(string inputPath);
        public Task<Yolov4InputData> ProcessImageForBoundingBoxPredictionAsync(string inputPath);
        public Task<MemoryStream> GetBlurriedImageAsync(string imagePath);
    }
}
