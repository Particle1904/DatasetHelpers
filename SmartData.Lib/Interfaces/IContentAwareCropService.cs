using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IContentAwareCropService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public float ScoreThreshold { get; set; }
        public float IouThreshold { get; set; }
        public float ExpansionPercentage { get; set; }

        public Task ProcessCroppedImage(string inputPath, string outputPath);
        public Task ProcessCroppedImage(string inputPath, string outputPath, Progress progress);
    }
}
