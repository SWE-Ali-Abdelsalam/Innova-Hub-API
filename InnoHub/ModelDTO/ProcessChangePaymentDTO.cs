namespace InnoHub.ModelDTO
{
    public class ProcessChangePaymentDTO
    {
        public int DealId { get; set; }
        public int ChangeRequestId { get; set; }
        public string Platform { get; set; } = "web"; // web or mobile
    }
}
