using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class ProcessTagsConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("randomizeTags")]
        public bool RandomizeTags { get; set; } = false;

        [JsonPropertyName("renameFiles")]
        public bool RenameFiles { get; set; } = false;

        [JsonPropertyName("applyRedudancyRemoval")]
        public bool ApplyRedudancyRemoval { get; set; } = true;

        [JsonPropertyName("consolidateTags")]
        public bool ConsolidateTags { get; set; } = false;
    }
}
