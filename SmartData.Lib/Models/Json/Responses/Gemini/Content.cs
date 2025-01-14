using System.Text.Json.Serialization;

namespace Models.Json.Responses.Gemini
{
    public class Content
    {
        [JsonPropertyName("parts")]
        public List<Part> Parts { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }
    }
}
