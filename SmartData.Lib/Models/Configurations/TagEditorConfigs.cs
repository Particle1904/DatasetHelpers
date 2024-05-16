using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class TagEditorConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("exactMatchesFiltering")]
        public bool ExactMatchesFiltering { get; set; } = false;
    }
}
