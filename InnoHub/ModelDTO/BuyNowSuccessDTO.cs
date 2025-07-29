namespace InnoHub.ModelDTO
{
    public class BuyNowSuccessDTO
    {
        public string PaymentIntentId { get; set; }
        public string ClientSecret { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int DeliveryMethodId { get; set; }
        public string UserComment { get; set; }
        public string UserEmail { get; set; }
    }
}
