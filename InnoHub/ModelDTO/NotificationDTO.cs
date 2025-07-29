namespace InnoHub.ModelDTO
{
    public class NotificationDTO
    {
        public int Id { get; set; }
        public int? DealId { get; set; }
        public string SenderName { get; set; }
        public string MessageText { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public string MessageType { get; set; }
    }
}
