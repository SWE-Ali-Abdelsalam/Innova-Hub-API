namespace InnoHub.ModelDTO.ML
{
    public class FlaskAIConfiguration
    {
        public string BaseUrl { get; set; } = "";
        public int Timeout { get; set; } = 30;
        public int RetryAttempts { get; set; } = 3;
        public int RetryDelay { get; set; } = 2;
        public bool EnableHealthChecks { get; set; } = true;
        public FlaskEndpoints Endpoints { get; set; } = new();
        public bool RequiredForOperation { get; set; } = true;
    }
}
