namespace InnoHub.ModelDTO
{
    public class GenerateContractDTO
    {
        public string InvestorName { get; set; }
        public string OwnerName { get; set; }
        public string BusinessName { get; set; }
        public decimal EstimatedPrice { get; set; }
        public decimal InvestmentAmount { get; set; }
        public decimal EquityPercentage { get; set; }
        public decimal PlatformFeePercentage { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
