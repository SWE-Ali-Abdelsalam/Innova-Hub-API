namespace InnoHub.ModelDTO
{
    public class PaymentRefundLogDTO
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal RefundAmount { get; set; }
        public string RefundId { get; set; }
        public string RefundStatus { get; set; }
        public DateTime RefundCreated { get; set; }
    }
}
