using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IAutoCaptionService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public Task GenerateCaptions(string inputPath, string outputPath);
        public Task GenerateCaptions(string inputPath, string outputPath, Progress progress);
    }
}
