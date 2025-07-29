using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class RecommendationRequestDTO
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Customer ID must be greater than 0")]
        [JsonPropertyName("customer_id")]
        public int CustomerId { get; set; }
    }
}
