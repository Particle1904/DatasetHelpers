using SixLabors.ImageSharp;

namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IUpscalerService
    {
        public Task UpscaleImageAndSaveAsync(string inputImagePath, string outputImagePath);
        public Task<Image> UpscaleImageAsync(string inputImagePath);
    }
}
