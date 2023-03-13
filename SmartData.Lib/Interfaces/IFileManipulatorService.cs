namespace SmartData.Lib.Interfaces
{
    public interface IFileManipulatorService
    {
        public void RenameAllToCrescent(string path);
        public void SortImages(string inputPath, string discardedOutputPath, string selectedOutputPath, int minimumSize = 512);
        public void CreateFolderIfNotExist(string folderName);
    }
}