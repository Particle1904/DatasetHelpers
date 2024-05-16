using SmartData.Lib.Enums;

using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class UpscaleImagesConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("upscalerModel")]
        public AvailableModels UpscalerModel { get; set; } = AvailableModels.SwinIR_x4;
    }
}
