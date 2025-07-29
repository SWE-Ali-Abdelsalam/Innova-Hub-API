using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class RecommendationItemDTO
    {
        [JsonPropertyName("item_id")]
        public int ItemId { get; set; }

        [JsonPropertyName("item_name")]
        public string ItemName { get; set; } = "";

        [JsonPropertyName("predicted_rating")]
        public double? PredictedRating { get; set; }

        [JsonPropertyName("confidence")]
        public bool? Confidence { get; set; }

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        // Additional properties for our API response
        public string? ProductName { get; set; }
        public string? ProductImageUrl { get; set; }
        public decimal? ProductPrice { get; set; }
        public string? AuthorName { get; set; }
        public bool IsAvailable { get; set; }
    }
}
