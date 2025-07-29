namespace InnoHub.ModelDTO.ML
{
    public class ProductSalesPredictionDTO
    {
        public string? ProductType { get; set; }
        public string? Season { get; set; }
        public string? MarketingChannel { get; set; }
        public double AdBudget { get; set; }
        public double? UnitPrice { get; set; }
        public int UnitsSold { get; set; }
    }
}
