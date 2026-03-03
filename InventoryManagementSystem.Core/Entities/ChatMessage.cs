using System;

namespace InventoryManagementSystem.Core.Entities
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;  // ← Change to string
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty; // ← Change to string
        public string ReceiverName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsRead { get; set; }
        public string? ConversationId { get; set; }
    }
}