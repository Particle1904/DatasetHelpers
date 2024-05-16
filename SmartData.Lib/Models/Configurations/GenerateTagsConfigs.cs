using SmartData.Lib.Enums;

using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class GenerateTagsConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("autoTaggerModel")]
        public AvailableModels AutoTaggerModel { get; set; } = AvailableModels.WDv3;

        private float _predictionsThreshold = 0.4f;
        [JsonPropertyName("predictionsThreshold")]
        public float PredictionsThreshold
        {
            get => _predictionsThreshold;
            set
            {
                _predictionsThreshold = Math.Clamp(value, 0.1f, 1.0f);
            }
        }

        [JsonPropertyName("applyRedudancyRemoval")]
        public bool ApplyRedudancyRemoval { get; set; } = true;

        [JsonPropertyName("appendToExistingFile")]
        public bool AppendToExistingFile { get; set; } = false;

        [JsonPropertyName("weightedCaptions")]
        public bool WeightedCaptions { get; set; } = false;
    }
}
