using Models.Json.Responses.Gemini;

using System.Text.Json.Serialization;

namespace SmartData.Lib.Models.Json.Responses.Gemini
{
    public class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate> Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata UsageMetadata { get; set; }

        [JsonPropertyName("modelVersion")]
        public string ModelVersion { get; set; }
    }
}
