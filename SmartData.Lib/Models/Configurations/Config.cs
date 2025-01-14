using Models.Configurations;

using System.Text.Json.Serialization;

namespace SmartData.Lib.Models.Configurations
{
    public class Config
    {
        [JsonPropertyName("galleryConfigs")]
        public GalleryConfigs GalleryConfigs { get; set; }

        [JsonPropertyName("sortImagesConfigs")]
        public SortImagesConfigs SortImagesConfigs { get; set; }

        [JsonPropertyName("contentAwareCropConfigs")]
        public ContentAwareCropConfigs ContentAwareCropConfigs { get; set; }

        [JsonPropertyName("manualCropConfigs")]
        public ManualCropConfigs ManualCropConfigs { get; set; }

        [JsonPropertyName("resizeImagesConfigs")]
        public ResizeImagesConfigs ResizeImagesConfigs { get; set; }

        [JsonPropertyName("upscaleImagesConfigs")]
        public UpscaleImagesConfigs UpscaleImagesConfigs { get; set; }

        [JsonPropertyName("generateTagsConfigs")]
        public GenerateTagsConfigs GenerateTagsConfigs { get; set; }

        [JsonPropertyName("geminiCaptionConfigs")]
        public GeminiCaptionConfigs GeminiCaptionConfigs { get; set; }

        [JsonPropertyName("processCaptionsConfigs")]
        public ProcessCaptionsConfigs ProcessCaptionsConfigs { get; set; }

        [JsonPropertyName("processTagsConfigs")]
        public ProcessTagsConfigs ProcessTagsConfigs { get; set; }

        [JsonPropertyName("tagEditorConfigs")]
        public TagEditorConfigs TagEditorConfigs { get; set; }

        [JsonPropertyName("extractSubsetConfigs")]
        public ExtractSubsetConfigs ExtractSubsetConfigs { get; set; }

        [JsonPropertyName("promptGeneratorConfigs")]
        public PromptGeneratorConfigs PromptGeneratorConfigs { get; set; }

        [JsonPropertyName("metadataViewerConfigs")]
        public MetadataViewerConfigs MetadataViewerConfigs { get; set; }
    }
}
