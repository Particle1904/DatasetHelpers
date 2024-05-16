using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class GalleryConfigs
    {
        [JsonPropertyName("inputPath")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("imageDisplaySize")]
        public int ImageDisplaySize { get; set; } = 380;
    }
}
