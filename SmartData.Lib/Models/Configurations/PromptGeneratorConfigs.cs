using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class PromptGeneratorConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("tagsToPrepend")]
        public string TagsToPrepend { get; set; } = string.Empty;

        [JsonPropertyName("tagsToAppend")]
        public string TagsToAppend { get; set; } = "masterpiece, best quality, absurdres";

        [JsonPropertyName("amountOfTags")]
        public int AmountOfTags { get; set; } = 15;

        [JsonPropertyName("amountOfPrompts")]
        public int AmountOfPrompts { get; set; } = 1000;
    }
}
