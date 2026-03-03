using System;

namespace InventoryManagementSystem.Core.Entities
{
    public class UserConnection
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;      // ← Change to string
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public bool IsOnline { get; set; }
    }
}