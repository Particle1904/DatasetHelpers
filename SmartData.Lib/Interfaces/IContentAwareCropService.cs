using SmartData.Lib.Enums;

namespace SmartData.Lib.Interfaces
{
    public interface IContentAwareCropService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public int LanczosRadius { get; set; }
        public bool ApplySharpen { get; set; }
        public double SharpenSigma { get; set; }
        public int MinimumResolutionForSigma { get; set; }
        public float ScoreThreshold { get; set; }
        public float IouThreshold { get; set; }
        public float ExpansionPercentage { get; set; }

        public Task ProcessCroppedImagesAsync(string inputPath, string outputPath, SupportedDimensions dimension);
    }
}
