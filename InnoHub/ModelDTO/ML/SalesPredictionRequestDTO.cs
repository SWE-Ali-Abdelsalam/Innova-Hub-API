using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SalesPredictionRequestDTO
    {
        [Required]
        [JsonPropertyName("product_type")]
        public string ProductType { get; set; } = ""; // Camera, Headphones, Laptop, Smartphone, TV, Tablet, Watch

        [Required]
        [JsonPropertyName("season")]
        public string Season { get; set; } = ""; // Fall, Spring, Summer, Winter

        [Required]
        [JsonPropertyName("marketing_channel")]
        public string MarketingChannel { get; set; } = ""; // Affiliate, Direct, Email, Search Engine, Social Media

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "Ad Budget must be greater than or equal to 0")]
        [JsonPropertyName("ad_budget")]
        public double AdBudget { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit Price must be greater than 0")]
        [JsonPropertyName("unit_price")]
        public double UnitPrice { get; set; }

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Units Sold must be greater than or equal to 0")]
        [JsonPropertyName("units_sold")]
        public int UnitsSold { get; set; }
    }
}
