// Ignore Spelling: Lanczos

using SmartData.Lib.Enums;
using SmartData.Lib.Models.MachineLearning;

namespace SmartData.Lib.Interfaces
{
    public interface IImageProcessorService
    {
        public int LanczosSamplerRadius { get; set; }
        public float SharpenSigma { get; set; }
        public bool ApplySharpen { get; set; }
        public int MinimumResolutionForSigma { get; set; }
        public Task<System.Drawing.Size> GetImageSizeAsync(string filePath);
        public Task CropImageAsync(string inputPath, string outputPath, List<DetectedPerson> results, float expansionPercentage, SupportedDimensions dimension);
        public Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension);
        public Task<WDInputData> ProcessImageForTagPredictionAsync(string inputPath);
        public Task<WDInputData> ProcessImageForTagPredictionAsync(Stream inputStream);
        public Task<JoyTagInputData> ProcessImageForJoyTagPredictionAsync(string inputPath);
        public Task<Yolov4InputData> ProcessImageForBoundingBoxPredictionAsync(string inputPath);
        public Task<MemoryStream> GetBlurredImageAsync(string inputPath);
        public Task<List<string>> ReadImageMetadataAsync(Stream imageStream);
        public Task CropImageAsync(string inputPath, string outputPath, System.Drawing.Point startingPosition, System.Drawing.Point endingPosition);
    }
}