using System;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagementSystem.Core.Entities
{
    public class Product : BaseEntity
    {
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [StringLength(50)]
        public string SKU { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int QuantityInStock { get; set; }

        [Range(0, int.MaxValue)]
        public int MinimumStockLevel { get; set; }

        // Foreign keys
        public int CategoryId { get; set; }
        public int? SupplierId { get; set; }

        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}