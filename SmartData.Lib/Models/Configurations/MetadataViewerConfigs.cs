using SmartData.Lib.Enums;

using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class MetadataViewerConfigs
    {
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

        [JsonPropertyName("autoTaggerModel")]
        public AvailableModels AutoTaggerModel { get; set; } = AvailableModels.WD14v2;
    }
}
