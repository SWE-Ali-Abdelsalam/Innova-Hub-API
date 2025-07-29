namespace InnoHub.ModelDTO.ML
{
    public class SpamDetectionSettings
    {
        public bool AutoBlockSpamUsers { get; set; } = false;
        public double SpamThreshold { get; set; } = 0.7;
        public bool CheckOnRegistration { get; set; } = true;
        public bool CheckOnDealCreation { get; set; } = true;
    }
}
