using SmartData.Lib.Helpers;

namespace SmartData.Lib.Interfaces
{
    public interface ITagProcessorService
    {
        public Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove);
        public Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove, Progress progress);
        public Task FindAndReplace(string inputFolderPath, string wordsToBeReplaced, string wordsToReplace);
        public Task FindAndReplace(string inputFolderPath, string wordsToBeReplaced, string wordsToReplace, Progress progress);
        public Task ProcessTagsReplacement(string inputFolderPath, string tagsToBeReplaced, string tagsToReplace);
        public Task ProcessTagsReplacement(string inputFolderPath, string tagsToBeReplaced, string tagsToReplace, Progress progress);
        public Task RandomizeTagsOfFiles(string inputFolderPath);
        public Task RandomizeTagsOfFiles(string inputFolderPath, Progress progress);
        public Task ApplyRedundancyRemovalToFiles(string inputFolderPath);
        public Task ApplyRedundancyRemovalToFiles(string inputFolderPath, Progress progress);
        public string CalculateListOfMostFrequentTags(string inputFolderPath);
        public string ApplyRedundancyRemoval(string tags);
        public string GetCommaSeparatedString(List<string> predictedTags);
    }
}
