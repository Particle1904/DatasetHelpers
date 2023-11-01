namespace SmartData.Lib.Interfaces
{
    public interface ITagProcessorService
    {
        public Task ProcessAllTagFiles(string inputFolderPath, string tagsToAdd, string tagsToEmphasize, string tagsToRemove);
        public Task ConsolidateTags(string inputFolderPath);
        public Task ConsolidateTagsAndLogEdgeCases(string inputFolderPath, ILoggerService loggerService);
        public Task FindAndReplace(string inputFolderPath, string wordsToBeReplaced, string wordsToReplace);
        public Task ProcessTagsReplacement(string inputFolderPath, string tagsToBeReplaced, string tagsToReplace);
        public Task RandomizeTagsOfFiles(string inputFolderPath);
        public Task ApplyRedundancyRemovalToFiles(string inputFolderPath);
        public string CalculateListOfMostFrequentTags(string inputFolderPath);
        public string ApplyRedundancyRemoval(string tags);
        public string GetCommaSeparatedString(List<string> predictedTags);
        public string[] GetTagsFromDataset(string inputFolderPath);
    }
}
