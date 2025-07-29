namespace InnoHub.ModelDTO
{
    public class NotificationStatsDTO
    {
        public int TotalNotifications { get; set; }
        public int UnreadNotifications { get; set; }
        public int ReadNotifications { get; set; }
        public double ReadPercentage { get; set; }
        public int TodayNotifications { get; set; }
        public int ThisWeekNotifications { get; set; }
        public Dictionary<string, int> NotificationsByType { get; set; } = new();
        public Dictionary<string, int> NotificationsByDay { get; set; } = new();
    }
}
