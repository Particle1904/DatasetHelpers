using SmartData.Lib.Enums;

namespace SmartData.Lib.Interfaces
{
    public interface IFileManipulatorService
    {
        public event EventHandler<string> DownloadMessageEvent;
        public bool IsDownloading { get; }
        public Task RenameAllToCrescentAsync(string inputPath, int startingNumberForFileNames = 1);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, SupportedDimensions dimension = SupportedDimensions.Resolution512x512);
        public void CreateFolderIfNotExist(string folderName);
        public Task BackupFilesAsync(string inputPath, string backupPath);
        public List<string> GetImageFiles(string inputPath);
        public List<string> GetFilteredImageFiles(string inputPath, string txtFileExtension, string wordsToFilter, bool exactMatchesOnly);
        public List<string> GetFilteredImageFiles(string inputPath, string txtFileExtension, string wordsToFilter);
        public Task DeleteFilesAsync(string inputPath, List<string> imageFiles);
        public string GetTextFromFile(string imageFilePath, string txtFileExtension);
        public void SaveTextToFile(string filePath, string textToSave);
        public Task CreateSubsetAsync(List<string> files, string outputPath);
        public Task DownloadModelFile(AvailableModels model);
        public bool FileNeedsToBeDownloaded(AvailableModels model);
    }
}