using System.Text.Json.Serialization;

namespace Models.Json.Responses.Gemini
{
    public class Candidate
    {
        [JsonPropertyName("content")]
        public Content Content { get; set; }

        [JsonPropertyName("finishReason")]
        public string FinishReason { get; set; }

        [JsonPropertyName("safetyRatings")]
        public List<SafetyRating> SafetyRatings { get; set; }

        [JsonPropertyName("avgLogprobs")]
        public double AvgLogprobs { get; set; }
    }
}
