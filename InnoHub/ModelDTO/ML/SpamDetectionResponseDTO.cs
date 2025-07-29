using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SpamDetectionResponseDTO
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("input_features")]
        public SpamDetectionRequestDTO InputFeatures { get; set; } = new();

        [JsonPropertyName("prediction")]
        public string Prediction { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        // Additional properties for our API
        public bool IsSpam => Prediction.ToLower() == "spam";
        public double ConfidenceScore { get; set; }
        public string RecommendedAction { get; set; } = "";
    }
}
