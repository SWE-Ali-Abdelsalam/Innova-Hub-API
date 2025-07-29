using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class MLHealthCheckResponseDTO
    {
        [JsonPropertyName("status_code")]
        public int StatusCode { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "";

        [JsonPropertyName("timestamp")]
        public string Timestamp { get; set; } = "";

        [JsonPropertyName("models_loaded")]
        public ModelsLoadedDTO ModelsLoaded { get; set; } = new();

        [JsonPropertyName("spam_preprocessors_loaded")]
        public SpamPreprocessorsDTO SpamPreprocessors { get; set; } = new();

        [JsonPropertyName("sales_preprocessors_loaded")]
        public SalesPreprocessorsDTO SalesPreprocessors { get; set; } = new();
    }
}
