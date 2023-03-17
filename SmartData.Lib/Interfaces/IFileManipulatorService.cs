using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IFileManipulatorService
    {
        public Task RenameAllToCrescentAsync(string inputPath);
        public Task RenameAllToCrescentAsync(string inputPath, Progress progress);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, Progress progress, int minimumSize = 512);
        public void CreateFolderIfNotExist(string folderName);
        public Task BackupFiles(string inputPath, string backupPath);
    }
}