using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IImageProcessorService
    {
        public Task ResizeImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension);
        public Task ResizeImagesAsync(string inputPath, string outputPath, Progress progress, SupportedDimensions dimension);
    }
}
