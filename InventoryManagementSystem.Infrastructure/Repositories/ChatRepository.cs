using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;
using InventoryManagementSystem.Core.Interfaces;
using InventoryManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagementSystem.Infrastructure.Repositories
{
    public class ChatRepository : IChatRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatMessage> SaveMessageAsync(ChatMessage message)
        {
            message.Timestamp = DateTime.UtcNow;

            var users = new[] { message.SenderId, message.ReceiverId }.OrderBy(id => id).ToList();
            message.ConversationId = $"{users[0]}_{users[1]}";

            await _context.ChatMessages.AddAsync(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<ChatMessage>> GetConversationAsync(string userId1, string userId2, int count = 50)
        {
            var users = new[] { userId1, userId2 }.OrderBy(id => id).ToList();
            var conversationId = $"{users[0]}_{users[1]}";

            return await _context.ChatMessages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.Timestamp)
                .Take(count)
                .OrderBy(m => m.Timestamp)
                .ToListAsync();
        }

        public async Task MarkMessagesAsReadAsync(string userId, string senderId)
        {
            var unreadMessages = await _context.ChatMessages
                .Where(m => m.ReceiverId == userId && m.SenderId == senderId && !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            return await _context.ChatMessages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
        }

        public async Task<List<(string UserId, string UserName, int UnreadCount)>> GetRecentChatsAsync(string userId)
        {
            var recentMessages = await _context.ChatMessages
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.Timestamp)
                .Take(100)
                .ToListAsync();

            var chatPartners = new Dictionary<string, (string UserName, int UnreadCount)>();

            foreach (var message in recentMessages)
            {
                string partnerId = message.SenderId == userId ? message.ReceiverId : message.SenderId;
                string partnerName = message.SenderId == userId ? message.ReceiverName : message.SenderName;

                if (!chatPartners.ContainsKey(partnerId))
                {
                    int unreadCount = await _context.ChatMessages
                        .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);

                    chatPartners[partnerId] = (partnerName, unreadCount);
                }
            }

            return chatPartners.Select(kvp => (kvp.Key, kvp.Value.UserName, kvp.Value.UnreadCount)).ToList();
        }

        public async Task AddUserConnectionAsync(UserConnection connection)
        {
            var existing = await _context.UserConnections
                .FirstOrDefaultAsync(u => u.UserId == connection.UserId);

            if (existing != null)
            {
                _context.UserConnections.Remove(existing);
            }

            await _context.UserConnections.AddAsync(connection);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserConnectionAsync(string connectionId)
        {
            var connection = await _context.UserConnections
                .FirstOrDefaultAsync(u => u.ConnectionId == connectionId);

            if (connection != null)
            {
                _context.UserConnections.Remove(connection);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsUserOnlineAsync(string userId)
        {
            return await _context.UserConnections
                .AnyAsync(u => u.UserId == userId && u.IsOnline);
        }

        public async Task<List<string>> GetOnlineUsersAsync()
        {
            return await _context.UserConnections
                .Where(u => u.IsOnline)
                .Select(u => u.UserId)
                .ToListAsync();
        }

        public async Task<string?> GetUserConnectionIdAsync(string userId)
        {
            var connection = await _context.UserConnections
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsOnline);

            return connection?.ConnectionId;
        }
    }
}