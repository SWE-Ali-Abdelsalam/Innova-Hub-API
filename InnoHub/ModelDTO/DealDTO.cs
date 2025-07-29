namespace InnoHub.ModelDTO
{
    public class DealDTO
    {
        public int DealId { get; set; }
        public string ProjectName { get; set; }
        //public string InvestorName { get; set; }
        public string OwnerName { get; set; }
        public decimal OfferMoney { get; set; }
        public decimal OfferDeal { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
        public decimal TotalProfit { get; set; }
        public DateTime? LastDistribution { get; set; }
        public int DurationInMonths { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public int? RemainingDays { get; set; }
    }
}
