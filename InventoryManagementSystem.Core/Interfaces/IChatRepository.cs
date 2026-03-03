using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InventoryManagementSystem.Core.Entities;

namespace InventoryManagementSystem.Core.Interfaces
{
    public interface IChatRepository
    {
        Task<ChatMessage> SaveMessageAsync(ChatMessage message);
        Task<List<ChatMessage>> GetConversationAsync(string userId1, string userId2, int count = 50);
        Task MarkMessagesAsReadAsync(string userId, string senderId);
        Task<int> GetUnreadCountAsync(string userId);
        Task<List<(string UserId, string UserName, int UnreadCount)>> GetRecentChatsAsync(string userId);

        // User connection tracking
        Task AddUserConnectionAsync(UserConnection connection);
        Task RemoveUserConnectionAsync(string connectionId);
        Task<bool> IsUserOnlineAsync(string userId);
        Task<List<string>> GetOnlineUsersAsync();
        Task<string?> GetUserConnectionIdAsync(string userId);
    }
}