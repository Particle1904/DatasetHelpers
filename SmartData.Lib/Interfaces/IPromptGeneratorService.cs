namespace SmartData.Lib.Interfaces
{
    public interface IPromptGeneratorService
    {
        public string GeneratePromptFromDataset(string[] tags, string prepedTags, string appendTags, int amountOfTags);
    }
}