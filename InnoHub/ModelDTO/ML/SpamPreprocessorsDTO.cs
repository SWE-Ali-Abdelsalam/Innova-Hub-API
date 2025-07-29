using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SpamPreprocessorsDTO
    {
        [JsonPropertyName("Platform_Interaction_encoder")]
        public bool PlatformInteractionEncoder { get; set; }

        [JsonPropertyName("Sales_Consistency_encoder")]
        public bool SalesConsistencyEncoder { get; set; }

        [JsonPropertyName("Label_encoder")]
        public bool LabelEncoder { get; set; }
    }
}
