using System.Text.Json.Serialization;

namespace Models.Json.Responses.Gemini
{
    public class SafetyRating
    {
        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("probability")]
        public string Probability { get; set; }
    }
}
