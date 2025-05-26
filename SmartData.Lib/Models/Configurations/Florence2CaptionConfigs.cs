using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class Florence2CaptionConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;
    }
}
