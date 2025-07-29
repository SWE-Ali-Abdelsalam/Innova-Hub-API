namespace InnoHub.ModelDTO
{
    public class PaymentFailedDTO
    {
        public string PaymentIntentId { get; set; }
        public string FailureReason { get; set; }
        public string UserEmail { get; set; }
    }
}
