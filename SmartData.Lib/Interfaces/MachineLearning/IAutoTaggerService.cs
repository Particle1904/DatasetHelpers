namespace SmartData.Lib.Interfaces.MachineLearning
{
    public interface IAutoTaggerService
    {
        public bool IsModelLoaded { get; }
        public string ModelPath { get; set; }
        public string TagsPath { get; set; }
        public float Threshold { get; set; }

        public Task GenerateTags(string inputPath, string outputPath, bool weightedCaptions = false);
        public Task GenerateTagsAndAppendToFile(string inputPath, string outputPath, bool weightedCaptions = false);
        public Task GenerateTagsAndKeepRedundant(string inputPath, string outputPath, bool appendToFile, bool weightedCaptions = false);
        public Task<string> InterrogateImageFromStream(Stream imageStream);
    }
}
