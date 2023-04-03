using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface ITagProcessorService
    {
        public Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove);
        public Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove, Progress progress);
        public Task ProcessTagsReplacement(string inputFolderPath, string tagsToReplace, string tagsToBeReplaced);
        public Task ProcessTagsReplacement(string inputFolderPath, string tagsToReplace, string tagsToBeReplaced, Progress progress);
        public Task RandomizeTagsOfFiles(string inputFolderPath);
        public Task RandomizeTagsOfFiles(string inputFolderPath, Progress progress);
        public Task CalculateListOfMostUsedTags(string inputFolderPath);
    }
}
