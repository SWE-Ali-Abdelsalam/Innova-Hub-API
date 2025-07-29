namespace InnoHub.ModelDTO
{
    public class PaymentStatusDTO
    {
        public int InvestmentId { get; set; }
        public bool IsPaymentProcessed { get; set; }
        public string PaymentStatus { get; set; }
        public string PaymentError { get; set; }
        public DateTime? PaymentProcessedAt { get; set; }
    }
}
