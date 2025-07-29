namespace InnoHub.ModelDTO
{
    public class GetDealsNeedAdminApprovalDTO
    {
        public int DealId { get; set; }
        public string BusinessName { get; set; }
        public string AuthorId { get; set; }
        public string AuthorName { get; set; }
        public string InvestorId { get; set; }
        public string InvestorName { get; set; }
        public decimal OfferMoney { get; set; }
        public decimal OfferDeal { get; set; }
        public decimal ManufacturingCost { get; set; }
        public decimal EstimatedPrice { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public bool AdminApproved { get; set; }
    }
}
