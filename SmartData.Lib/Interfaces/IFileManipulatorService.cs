using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface IFileManipulatorService
    {
        public void RenameAllToCrescent(string path);
        public void RenameAllToCrescent(string path, Progress progress);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512);
        public Task SortImagesAsync(string inputPath, string discardedOutputPath, string selectedOutputPath, Progress progress, int minimumSize = 512);
        public void CreateFolderIfNotExist(string folderName);
        public void BackupFiles(string inputPath, string backupPath);
    }
}