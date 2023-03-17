using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IAutoTaggerService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public string TagsPath { get; set; }
        public float Threshold { get; set; }

        public Task GenerateTags(string inputPath, string outputPath);
        public Task GenerateTags(string inputPath, string outputPath, Progress progess);
    }
}
