namespace InnoHub.ModelDTO.ML
{
    public class MLFeaturesConfiguration
    {
        public bool EnableRecommendations { get; set; } = true;
        public bool EnableSpamDetection { get; set; } = true;
        public bool EnableSalesPrediction { get; set; } = true;
        public bool RequireFlaskForAll { get; set; } = true;
        public RecommendationSettings RecommendationSettings { get; set; } = new();
        public SpamDetectionSettings SpamDetectionSettings { get; set; } = new();
        public SalesPredictionSettings SalesPredictionSettings { get; set; } = new();
    }
}
