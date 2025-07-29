namespace InnoHub.ModelDTO
{
    public class NotificationPreferencesDTO
    {
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool DealUpdates { get; set; } = true;
        public bool PaymentAlerts { get; set; } = true;
        public bool AdminMessages { get; set; } = true;
        public bool MarketingEmails { get; set; } = false;
    }
}
