namespace InnoHub.ModelDTO.ML
{
    public class SalesPredictionSettings
    {
        public bool ShowToBusinessOwners { get; set; } = true;
        public int CacheExpirationHours { get; set; } = 24;
    }
}
