namespace Models.Configurations
{
    public class DownloadNotification
    {
        public string NotificationMessage { get; set; }
        public bool PlayNotificationSound { get; set; }

        public DownloadNotification(string notificationMessage, bool playNotificationSound)
        {
            NotificationMessage = notificationMessage;
            PlayNotificationSound = playNotificationSound;
        }
    }
}
