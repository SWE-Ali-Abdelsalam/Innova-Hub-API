namespace InnoHub.ModelDTO
{
    public class DealResponse
    {
        public int DealId { get; set; }
        public string BusinessName { get; set; }
        public string Description { get; set; }
        public decimal OfferMoney { get; set; }
        public decimal OfferDeal { get; set; }
        public List<string> Pictures { get; set; } = new List<string>();
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public decimal ManufacturingCost { get; set; }
        public decimal EstimatedPrice { get; set; }
        public bool IsApproved { get; set; }
    }

}
