using System;

namespace InventoryManagementSystem.Core.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductSKU { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime AddedAt { get; set; }

        // Navigation properties
        public virtual Cart? Cart { get; set; }
        public virtual Product? Product { get; set; }

        // Calculated property
        public decimal TotalPrice => UnitPrice * Quantity;
    }
}