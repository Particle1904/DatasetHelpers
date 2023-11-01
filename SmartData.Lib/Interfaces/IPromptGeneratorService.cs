namespace SmartData.Lib.Interfaces
{
    public interface IPromptGeneratorService
    {
        public string GeneratePromptFromDataset(string[] tags, string prepedTags, string appendTags, int amountOfTags);
        public Task GeneratePromptsAndSaveToFile(string outputFile, string[] tags, string prependTags,
            string appendTags, int amountOfTags, int amountOfPrompts);
    }
}