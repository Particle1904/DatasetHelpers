namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IUpscalerService
    {
        public Task UpscaleImageAsync(string inputImagePath, string outputImagePath);
    }
}
