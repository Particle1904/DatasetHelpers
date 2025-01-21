namespace SmartData.Lib.Interfaces
{
    public interface IGeminiService
    {
        public string ApiKey { get; set; }
        public string SystemInstructions { get; set; }
        public bool FreeApi { get; set; }
        public Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, string failedOutputFolderPath, string prompt);
    }
}
