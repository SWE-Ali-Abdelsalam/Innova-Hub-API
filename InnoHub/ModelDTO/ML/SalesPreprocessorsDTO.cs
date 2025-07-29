using System.Text.Json.Serialization;

namespace InnoHub.ModelDTO.ML
{
    public class SalesPreprocessorsDTO
    {
        [JsonPropertyName("product_type_encoder")]
        public bool ProductTypeEncoder { get; set; }

        [JsonPropertyName("marketing_channel_encoder")]
        public bool MarketingChannelEncoder { get; set; }

        [JsonPropertyName("season_encoder")]
        public bool SeasonEncoder { get; set; }

        [JsonPropertyName("sales_scaler")]
        public bool SalesScaler { get; set; }
    }
}
