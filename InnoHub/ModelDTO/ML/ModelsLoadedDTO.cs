using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class ModelsLoadedDTO
    {
        [JsonPropertyName("recommendation")]
        public bool Recommendation { get; set; }

        [JsonPropertyName("spam_detection")]
        public bool SpamDetection { get; set; }

        [JsonPropertyName("sales_prediction")]
        public bool SalesPrediction { get; set; }
    }
}
