namespace InnoHub.ModelDTO
{
    public class BuyNowFailedDTO
    {
        public string PaymentIntentId { get; set; }
        public string UserEmail { get; set; }
        public string FailureReason { get; set; }
    }
}
