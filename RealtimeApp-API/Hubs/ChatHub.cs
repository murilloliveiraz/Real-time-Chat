using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RealtimeApp_API.Models;
using RealtimeApp_API.Repositories;
using System.Text.Json;

namespace RealtimeApp_API.Hubs
{
    [Authorize]
    public class ChatHub: Hub
    {
        private readonly UserRepository _userRepository;
        private readonly IProducer<Null, string> _kafkaProducer;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly string MESSAGESTOPIC = "chat-messages";
        private readonly string ONLINETOPIC = "online-status";

        public ChatHub(UserRepository userRepository, IProducer<Null, string> kafkaProducer, IHubContext<ChatHub> hubContext)
        {
            _userRepository = userRepository;
            _kafkaProducer = kafkaProducer;
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;

            if (userId != null)
            {
                _userRepository.SetUserOnlineStatus(userId, true);
                Console.WriteLine($"User Connected: {userId}");

                await PublishOnlineStatusToKafka(userId, true);
                await BroadcastOnlineUsers();
            }

            await base.OnConnectedAsync();
        }
        
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;

            if (userId != null)
            {
                _userRepository.SetUserOnlineStatus(userId, false);
                Console.WriteLine($"User Disconnected: {userId}");

                await PublishOnlineStatusToKafka(userId, false);
                await BroadcastOnlineUsers();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendPrivateMessage(string recipientUserId, string messageContent)
        {
            var senderUserId = Context.UserIdentifier;
            var senderUsername = Context.User?.Identity?.Name;

            if (string.IsNullOrEmpty(senderUserId) || string.IsNullOrEmpty(senderUsername))
            {
                await Clients.Caller.SendAsync("ReceiveSystemMessage", "Error: Sender not identified.");
                return;
            }

            var recipientUser = _userRepository.GetUserByUserId(recipientUserId);
            if (recipientUser == null)
            {
                await Clients.Caller.SendAsync("ReceiveSystemMessage", "Error: Recipient not identified.");
                return;
            }

            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                SenderId = senderUserId,
                SenderUsername = senderUsername,
                RecipientId = recipientUser.UserId,
                RecipientUsername = recipientUser.Username,
                Content = messageContent,
                Timestamp = DateTimeOffset.UtcNow
            };

            var messageJson = JsonSerializer.Serialize(chatMessage);
            await _kafkaProducer.ProduceAsync(MESSAGESTOPIC, new Message<Null, string> { Value = messageJson });

            await Clients.Caller.SendAsync("ReceiveSystemMessage", $"Message sent to {recipientUser.Username}.");
        }

        private async Task PublishOnlineStatusToKafka(string userId, bool isOnline)
        {
            var statusUpdate = new
            {
                UserId = userId,
                Username = _userRepository.GetUserByUserId(userId)?.Username,
                IsOnline = isOnline
            };

            var statusJson = JsonSerializer.Serialize(statusUpdate);
            await _kafkaProducer.ProduceAsync(ONLINETOPIC, new Message<Null, string> { Value = statusJson });
        }

        private async Task BroadcastOnlineUsers()
        {
            var onlineUsers = _userRepository.GetOnlineUsers()
                                                .Select(u => new { u.UserId, u.Username})
                                                .ToList();
            await _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", onlineUsers);
        }
    }
}
