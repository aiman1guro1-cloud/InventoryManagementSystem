using System;
using System.Collections.Generic;

namespace InventoryManagementSystem.Core.Entities
{
    public class Cart
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation property
        public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Calculated properties (not stored in DB)
        public decimal Subtotal => CartItems?.Sum(item => item.TotalPrice) ?? 0;
        public decimal TaxAmount => Subtotal * 0.10m; // 10% tax
        public decimal Total => Subtotal + TaxAmount;
        public int TotalItems => CartItems?.Sum(item => item.Quantity) ?? 0;
    }
}