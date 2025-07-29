using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class RecommendationResponseDTO
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }

        [JsonPropertyName("user_type")]
        public string UserType { get; set; } = "";

        [JsonPropertyName("recommendations")]
        public List<RecommendationItemDTO> Recommendations { get; set; } = new();

        [JsonPropertyName("model_used")]
        public string ModelUsed { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";
    }
}
