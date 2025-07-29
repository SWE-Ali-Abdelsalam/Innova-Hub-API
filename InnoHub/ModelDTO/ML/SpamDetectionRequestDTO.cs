using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SpamDetectionRequestDTO
    {
        [Required]
        [Range(0, 1, ErrorMessage = "Profile Completeness must be between 0 and 1")]
        [JsonPropertyName("Profile_Completeness")]
        public double ProfileCompleteness { get; set; }

        [Required]
        [JsonPropertyName("Sales_Consistency")]
        public string SalesConsistency { get; set; } = ""; // low, medium, high

        [Required]
        [Range(0, 1, ErrorMessage = "Customer Feedback must be between 0 and 1")]
        [JsonPropertyName("Customer_Feedback")]
        public double CustomerFeedback { get; set; }

        [Required]
        [Range(0, 1, ErrorMessage = "Transaction History must be between 0 and 1")]
        [JsonPropertyName("Transaction_History")]
        public double TransactionHistory { get; set; }

        [Required]
        [JsonPropertyName("Platform_Interaction")]
        public string PlatformInteraction { get; set; } = ""; // low, medium, high
    }
}
