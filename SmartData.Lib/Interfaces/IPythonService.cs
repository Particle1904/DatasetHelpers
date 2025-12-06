namespace SmartData.Lib.Interfaces
{
    public interface IPythonService
    {
        public bool IsInitialized { get; }
        public void InitializePython();
        public void DownloadPythonPackages();
        public Task<string> GenerateContent(string base64Image, string prompt, string geminiApiKey, string systemInstructions, string modelName = "gemini-2.0-flash-lite");
    }
}
