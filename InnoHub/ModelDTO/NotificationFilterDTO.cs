namespace InnoHub.ModelDTO
{
    public class NotificationFilterDTO
    {
        public string? MessageType { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int? DealId { get; set; }
        public string? SenderId { get; set; }
    }
}
