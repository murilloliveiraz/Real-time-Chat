namespace RealtimeApp_API.Models
{
    public class User
    {
        public string UserId { get; set; } = Guid.NewGuid().ToString();
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
    }
}
