using SmartData.Lib.Enums;

using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class SortImagesConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("discardedFolder")]
        public string DiscardedFolder { get; set; } = string.Empty;

        [JsonPropertyName("dimensionSizeForDiscarded")]
        public SupportedDimensions DimensionSizeForDiscarded { get; set; } = SupportedDimensions.Resolution1024x1024;

        [JsonPropertyName("backupFolder")]
        public string BackupFolder { get; set; } = string.Empty;

        [JsonPropertyName("backupBeforeProcessing")]
        public bool BackupBeforeProcessing { get; set; } = true;
    }
}
