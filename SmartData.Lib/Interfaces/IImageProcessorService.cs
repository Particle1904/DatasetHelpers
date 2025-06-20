// Ignore Spelling: Lanczos

using Models;
using Models.MachineLearning;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using SmartData.Lib.Enums;
using SmartData.Lib.Models.MachineLearning;
using SmartData.Lib.Models.MachineLearning.SAM2;

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
        public Task<UpscalerInputData> ProcessImageForUpscalingAsync(string inputPath);
        public Task<LaMaInputData> ProcessImageForInpaintAsync(string inputPath, string inputMaskPath);
        public Task<TileData[]> ProcessImageForTileInpaintAsync(string inputPath, string inputMaskPath, int tileSize = 512, int overlap = 126);
        public Task<SAM2EncoderInputData> ProcessImageForSAM2EncodingAsync(string inputPath);
        public void SaveUpscaledImage(string outputPath, UpscalerOutputData outputData);
        public void SaveInpaintedImage(string outputPath, LaMaInputData inputData, LaMaOutputData outputData);
        public void SaveInpaintedImage(string outputPath, LaMaOutputData[] outputData, System.Drawing.Size originalSize, int tileSize = 512, int overlap = 126);
        public Image GetUpscaledImage(UpscalerOutputData outputData);
        public Task<MemoryStream> GetBlurredImageAsync(string inputPath);
        public MemoryStream CreateImageMask(int width, int height);
        public MemoryStream DrawCircleOnMask(MemoryStream maskStream, Point position, float radius, Color color);
        public Task<List<string>> ReadImageMetadataAsync(Stream imageStream);
        public Task CropImageAsync(string inputPath, string outputPath, System.Drawing.Point startingPosition, System.Drawing.Point endingPosition);
        public Task<string> GetBase64ImageAsync(string inputPath);
        public Task SaveSAM2MaskAsync(SAM2DecoderOutputData SAM2masks, string outputPath, int dilationSizeInPixels = 2);
        public Image<L8> CreateSAM2Mask(SAM2DecoderOutputData SAM2masks);
        public Task CombineListOfMasksAsync(List<Image<L8>> masks, string outputPath, int dilationSizeInPixels = 2);
    }
}