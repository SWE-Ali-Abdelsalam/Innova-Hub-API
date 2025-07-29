namespace InnoHub.ModelDTO
{
    public class ChangePaymentResponseDTO
    {
        public string Message { get; set; }
        public bool RequiresPayment { get; set; }
        public decimal? PaymentAmount { get; set; }
        public string? PaymentDirection { get; set; } // "investor_pays" or "refund_to_investor"
        public string? PaymentUrl { get; set; } // للويب
        public string? ClientSecret { get; set; } // للموبايل
        public string? PaymentIntentId { get; set; }
        public int ChangeRequestId { get; set; }
    }
}
