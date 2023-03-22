using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;
using SmartData.Lib.Models;

namespace SmartData.Lib.Interfaces
{
    public interface IImageProcessorService
    {
        public Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension);
        public Task ResizeImagesAsync(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension);
        public Task<InputData> ProcessImageForTagPrediction(string inputPath);
        public Stream GetBlurriedImage(string imagePath);
    }
}
