﻿using System.Text.Json.Serialization;

namespace Models.Configurations
{
    public class GeminiCaptionConfigs
    {
        [JsonPropertyName("inputFolder")]
        public string InputFolder { get; set; } = string.Empty;

        [JsonPropertyName("outputFolder")]
        public string OutputFolder { get; set; } = string.Empty;

        [JsonPropertyName("failedFolder")]
        public string FailedFolder { get; set; } = string.Empty;

        [JsonPropertyName("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [JsonPropertyName("freeApi")]
        public bool FreeApi { get; set; } = true;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;

        [JsonPropertyName("systemInstructions")]
        public string SystemInstructions { get; set; } = string.Empty;
    }
}
