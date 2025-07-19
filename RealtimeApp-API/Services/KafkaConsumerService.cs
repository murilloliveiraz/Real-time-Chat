
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;
using RealtimeApp_API.Hubs;
using RealtimeApp_API.Repositories;
using RealtimeApp_API.Models;

namespace RealtimeApp_API.Services
{
    public class KafkaConsumerService : BackgroundService
    {
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly UserRepository _userRepository;
        private readonly string _kafkaBootstrapServers;
        private readonly string _consumerGroupId;
        private readonly string[] _topicToSubscribe;

        public KafkaConsumerService(IHubContext<ChatHub> hubContext, UserRepository userRepository)
        {
            _hubContext = hubContext;
            _userRepository = userRepository;
            _kafkaBootstrapServers = "localhost:9092";
            _consumerGroupId = "chat-kafka-consumer-group";
            _topicToSubscribe = new[] { "online-status", "chat-messages" };
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => StartConsumerLoop(stoppingToken), stoppingToken);
        }

        private void StartConsumerLoop(CancellationToken stoppingToken)
        {
            var config = new ConsumerConfig
            {
                GroupId = _consumerGroupId,
                BootstrapServers = _kafkaBootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = true,
                EnableAutoOffsetStore = true 
            };

            using (var consumer = new ConsumerBuilder<Ignore, string>(config).Build())
            {
                consumer.Subscribe(_topicToSubscribe);

                try
                {
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        try
                        {
                            var consumeResult = consumer.Consume(stoppingToken);
                            var topic = consumeResult.Topic;
                            var messageValue = consumeResult.Message.Value;
                            Console.WriteLine($"Received Kafka message from topic - {topic}: {messageValue}");

                            switch (topic)
                            {
                                case "chat-messages":
                                    ProcessChatMessage(messageValue);
                                    break;
                                case "online-status":
                                    ProcessOnlineStatusUpdate(messageValue);
                                    break;
                                default:
                                    Console.WriteLine($"Unknown topic: {topic}");
                                    break;
                            }
                        }
                        catch (ConsumeException e)
                        {
                            Console.WriteLine($"Error consuming message: {e.Error.Reason}");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Kafka consumer stopped.");
                }
                finally
                {
                    consumer.Close();
                }
            }
        }

        private async void ProcessChatMessage(string messageJson)
        {
            try
            {
                var chatMessage = JsonSerializer.Deserialize<ChatMessage>(messageJson);
                if (chatMessage == null)
                    return;

                await _hubContext.Clients.User(chatMessage.RecipientId).SendAsync("ReceivePrivateMessage", chatMessage);
                await _hubContext.Clients.User(chatMessage.SenderId).SendAsync("ReceivePrivateMessage", chatMessage);

                Console.WriteLine($"Delivered chat message from {chatMessage.SenderUsername} to {chatMessage.RecipientUsername}");
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"Error deserializing chat message: {exception.Message}");
            }
        }

        private async void ProcessOnlineStatusUpdate(string statusJson)
        {
            try
            {
                var statusUpdate = JsonSerializer.Deserialize<StatusUpdate>(statusJson);
                if (statusUpdate == null)
                    return;

                string userId = statusUpdate.UserId;
                bool isOnline = statusUpdate.IsOnline;

                _userRepository.SetUserOnlineStatus(userId, isOnline);
                Console.WriteLine($"User '{userId}' is now {(isOnline ? "online" : "offline")}.");

                // Broadcast the updated list of online users to all clients
                var onlineUsers = _userRepository.GetOnlineUsers()
                                                .Select(u => new { u.UserId, u.Username })
                                                .ToList();

                await _hubContext.Clients.All.SendAsync("ReceiveOnlineUsers", onlineUsers);
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"Error deserializing online status: {exception.Message}");
            }
        }
    }
}
