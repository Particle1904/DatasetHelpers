using System.Text.Json.Serialization;

namespace Models.Json.Responses.Gemini
{
    public class Part
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
