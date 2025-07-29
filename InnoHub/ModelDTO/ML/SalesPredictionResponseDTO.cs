using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SalesPredictionResponseDTO
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("predicted_sales_revenue")]
        public double PredictedSalesRevenue { get; set; }

        [JsonPropertyName("input_features")]
        public SalesPredictionRequestDTO InputFeatures { get; set; } = new();

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        // Additional properties for business insights
        public double ProfitMargin { get; set; }
        public double ROI { get; set; }
        public string PerformanceCategory { get; set; } = ""; // Excellent, Good, Average, Poor
        public List<string> Recommendations { get; set; } = new();
    }
}
