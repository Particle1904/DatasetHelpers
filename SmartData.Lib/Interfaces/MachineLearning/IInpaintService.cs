namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IInpaintService
    {
        public Task InpaintImageAsync(string inputImagePath, string inputMaskPath, string outputImagePath);
        public Task InpaintImageTilesAsync(string inputImagePath, string inputMaskPath, string outputImagePath, int tileSize = 512, int overlap = 126);
        public Task InpaintImagesAsync(string inputFolderPath, string outputFolderPath);
    }
}
