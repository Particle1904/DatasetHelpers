using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class ProcessTagsConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("tagsToAdd")]
        public string TagsToAdd { get; set; } = string.Empty;

        [JsonPropertyName("tagsToEmphasize")]
        public string TagsToEmphasize { get; set; } = string.Empty;

        [JsonPropertyName("tagsToRemove")]
        public string TagsToRemove { get; set; } = string.Empty;

        [JsonPropertyName("tagsToUpdate")]
        public string TagsToUpdate { get; set; } = string.Empty;

        [JsonPropertyName("newTags")]
        public string NewTags { get; set; } = string.Empty;

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
