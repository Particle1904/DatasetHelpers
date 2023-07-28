using SmartData.Lib.Enums;
using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IFileManipulatorService
    {
        public Task RenameAllToCrescentAsync(string inputPath);
        public Task RenameAllToCrescentAsync(string inputPath, Progress progress);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, SupportedDimensions dimension = SupportedDimensions.Resolution512x512);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, Progress progress, SupportedDimensions dimension = SupportedDimensions.Resolution512x512);
        public void CreateFolderIfNotExist(string folderName);
        public Task BackupFiles(string inputPath, string backupPath);
        public List<string> GetImageFiles(string inputPath);
        public List<string> GetFilteredImageFiles(string inputPath, string txtFileExtension, string wordsToFilter);
        public string GetTextFromFile(string imageFilePath, string txtFileExtension);
        public void SaveTextForImage(string filePath, string textToSave);
    }
}