namespace VehicleAPI.DTOs.Response
{
    public class NotificationDTO
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Time { get; set; }
        public bool Unread { get; set; }
    }

    public class NotificationsResponseDTO
    {
        public List<NotificationDTO> Notifications { get; set; } = new();
        public int UnreadCount { get; set; }
    }
}