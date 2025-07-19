namespace RealtimeApp_API.Models
{
    public class ChatMessage
    {
        public string Id { get; set; } = string.Empty;
        public string SenderId { get; set; } = string.Empty;
        public string SenderUsername { get; set; } = string.Empty;
        public string RecipientId { get; set; } = string.Empty;
        public string RecipientUsername { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }
}
