namespace SmartData.Lib.Interfaces
{
    public interface IGeminiService
    {
        public string ApiKey { get; set; }
        public string SystemInstructions { get; set; }
        public Task CaptionImagesAsync(string inputFolderPath, string outputFolderPath, string prompt);
    }
}
