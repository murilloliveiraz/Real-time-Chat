using RealtimeApp_API.Models;

namespace RealtimeApp_API.Repositories
{
    public class UserRepository
    {
        private readonly List<User> _users = new List<User>();

        public UserRepository()
        {
            AddInitialUsers();
        }

        private void AddInitialUsers()
        {
            _users.Add(new User { Username = "alice", PasswordHash = "password_hashed_alice" });
            _users.Add(new User { Username = "bob", PasswordHash = "password_hashed_bob" });
            _users.Add(new User { Username = "charlie", PasswordHash = "password_hashed_charlie" });
        }

        public User? GetUserByUsername(string username) => _users.FirstOrDefault(u => u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        
        public User? GetUserByUserId(string userId) => _users.FirstOrDefault(u => u.UserId == userId);
        
        public List<User> GetOnlineUsers() => _users.Where(u => u.IsOnline).ToList();
        
        public void SetUserOnlineStatus(string userId, bool isOnline)
        {
            var user = _users.FirstOrDefault(u => u.UserId == userId);
            if (user != null)
            {
                user.IsOnline = isOnline;
            }
        }
    }
}
