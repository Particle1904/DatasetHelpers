using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class ExtractSubsetConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("searchTxt")]
        public bool SearchTxt { get; set; } = true;

        [JsonPropertyName("searchCaption")]
        public bool SearchCaption { get; set; } = true;

        [JsonPropertyName("exactMatchesFiltering")]
        public bool ExactMatchesFiltering { get; set; } = false;
    }
}
