using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class ProcessCaptionsConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;
    }
}
