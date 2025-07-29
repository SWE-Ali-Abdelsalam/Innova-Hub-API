namespace InnoHub.ModelDTO
{
    public class PaymentSuccessDTO
    {
        public string PaymentIntentId { get; set; }
        public string ClientSecret { get; set; }
        public int DeliveryMethodId { get; set; }
        public string UserComment { get; set; }
        public string UserEmail { get; set; }
    }
}
