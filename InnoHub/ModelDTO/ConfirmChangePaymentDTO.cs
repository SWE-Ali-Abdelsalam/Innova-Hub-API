namespace InnoHub.ModelDTO
{
    public class ConfirmChangePaymentDTO
    {
        public int DealId { get; set; }
        public int ChangeRequestId { get; set; }
        public string? PaymentIntentId { get; set; } // للموبايل
        public string? SessionId { get; set; } // للويب
    }
}
