namespace InnoHub.ModelDTO.ML
{
    public class RecommendationSettings
    {
        public int MaxRecommendations { get; set; } = 10;
        public int CacheExpirationMinutes { get; set; } = 30;
        public bool FallbackToPopular { get; set; } = false;
    }
}
