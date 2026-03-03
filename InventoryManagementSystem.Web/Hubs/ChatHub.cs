using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InventoryManagementSystem.Web.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private readonly IChatRepository _chatRepository;
        private readonly IUserRepository _userRepository;

        public ChatHub(IChatRepository chatRepository, IUserRepository userRepository)
        {
            _chatRepository = chatRepository;
            _userRepository = userRepository;
        }

        // ✅ SINGLE OnConnectedAsync method (merged both versions)
        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = Context.User?.Identity?.Name ?? "Unknown";

            if (!string.IsNullOrEmpty(userId))
            {
                // Store in memory
                _userConnections[userId] = Context.ConnectionId;

                // Save to database
                await _chatRepository.AddUserConnectionAsync(new UserConnection
                {
                    UserId = userId,
                    ConnectionId = Context.ConnectionId,
                    ConnectedAt = DateTime.UtcNow,
                    IsOnline = true
                });

                // Send all offline messages
                await SendOfflineMessages(userId);

                // Notify others
                await Clients.Others.SendAsync("UserOnline", userId, userName);

                // Send unread count
                var unreadCount = await _chatRepository.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);

                Console.WriteLine($"User {userName} connected with ID: {userId}");
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = _userConnections.FirstOrDefault(x => x.Value == Context.ConnectionId).Key;

            if (!string.IsNullOrEmpty(userId))
            {
                _userConnections.TryRemove(userId, out _);
                await _chatRepository.RemoveUserConnectionAsync(Context.ConnectionId);
                await Clients.Others.SendAsync("UserOffline", userId);

                Console.WriteLine($"User {userId} disconnected");
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ✅ Method to send offline messages
        private async Task SendOfflineMessages(string userId)
        {
            try
            {
                var recentChats = await _chatRepository.GetRecentChatsAsync(userId);

                foreach (var chat in recentChats)
                {
                    var messages = await _chatRepository.GetConversationAsync(userId, chat.UserId, 20);

                    var unreadMessages = messages.Where(m => m.ReceiverId == userId && !m.IsRead).ToList();

                    foreach (var message in unreadMessages)
                    {
                        await Clients.Caller.SendAsync("ReceiveMessage",
                            message.SenderId,
                            message.SenderName,
                            message.Message,
                            message.Timestamp);
                    }

                    await _chatRepository.MarkMessagesAsReadAsync(userId, chat.UserId);
                }

                Console.WriteLine($"Sent offline messages to user {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending offline messages: {ex.Message}");
            }
        }

        public async Task SendPrivateMessage(string receiverId, string message)
        {
            try
            {
                Console.WriteLine($"===== SEND MESSAGE ATTEMPT =====");
                Console.WriteLine($"Receiver ID: {receiverId}");

                var senderId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                var senderName = Context.User?.Identity?.Name ?? "Unknown";

                Console.WriteLine($"Sender ID: {senderId}");
                Console.WriteLine($"Sender Name: {senderName}");

                if (string.IsNullOrEmpty(senderId) || string.IsNullOrEmpty(receiverId) || string.IsNullOrEmpty(message))
                {
                    await Clients.Caller.SendAsync("Error", "Invalid message parameters");
                    return;
                }

                // Get receiver
                var receiver = await _userRepository.GetByIdAsync(receiverId);
                if (receiver == null)
                {
                    Console.WriteLine($"Receiver not found: {receiverId}");
                    await Clients.Caller.SendAsync("Error", "Receiver not found");
                    return;
                }

                // Save message
                var chatMessage = new ChatMessage
                {
                    SenderId = senderId,
                    SenderName = senderName,
                    ReceiverId = receiverId,
                    ReceiverName = receiver.UserName ?? "Unknown",
                    Message = message,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false
                };

                await _chatRepository.SaveMessageAsync(chatMessage);
                Console.WriteLine("✓ Message saved to database");

                // Send to receiver if online
                if (_userConnections.TryGetValue(receiverId, out var connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("ReceiveMessage", senderId, senderName, message, DateTime.UtcNow);
                    await Clients.Caller.SendAsync("MessageDelivered", receiverId);
                    Console.WriteLine("✓ Message delivered to receiver");
                }
                else
                {
                    await Clients.Caller.SendAsync("MessageSaved", receiverId);
                    Console.WriteLine("✓ Message saved for offline user");
                }

                // Update unread count
                var unreadCount = await _chatRepository.GetUnreadCountAsync(receiverId);
                if (_userConnections.TryGetValue(receiverId, out connectionId))
                {
                    await Clients.Client(connectionId).SendAsync("UpdateUnreadCount", unreadCount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                await Clients.Caller.SendAsync("Error", $"Server error: {ex.Message}");
            }
        }

        public async Task MarkMessagesAsRead(string senderId)
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                await _chatRepository.MarkMessagesAsReadAsync(userId, senderId);
                var unreadCount = await _chatRepository.GetUnreadCountAsync(userId);
                await Clients.Caller.SendAsync("UpdateUnreadCount", unreadCount);
            }
        }

        public async Task GetConversation(string userId)
        {
            var currentUserId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(currentUserId) && !string.IsNullOrEmpty(userId))
            {
                var messages = await _chatRepository.GetConversationAsync(currentUserId, userId);
                await Clients.Caller.SendAsync("LoadConversation", messages);
            }
        }

        public async Task UserTyping(string receiverId, bool isTyping)
        {
            var senderId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(senderId) && _userConnections.TryGetValue(receiverId, out var connectionId))
            {
                await Clients.Client(connectionId).SendAsync("UserTyping", senderId, isTyping);
            }
        }

        public async Task GetOnlineUsers()
        {
            var onlineUsers = await _chatRepository.GetOnlineUsersAsync();
            await Clients.Caller.SendAsync("OnlineUsersList", onlineUsers);
        }
    }
}