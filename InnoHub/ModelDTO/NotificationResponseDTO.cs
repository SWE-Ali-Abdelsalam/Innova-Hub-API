namespace InnoHub.ModelDTO
{
    public class NotificationResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }
        public int? UnreadCount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
