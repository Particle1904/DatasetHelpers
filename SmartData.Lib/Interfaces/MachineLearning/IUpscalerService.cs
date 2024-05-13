using SixLabors.ImageSharp;

using SmartData.Lib.Enums;

namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IUpscalerService
    {
        public Task UpscaleImagesAsync(string inputFolderPath, string outputFolderPath, AvailableModels model);
    }
}
