using System.Collections.Generic;
using InventoryManagementSystem.Core.Entities;

namespace InventoryManagementSystem.Web.Models
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }
        public int LowStockProducts { get; set; }
        public IEnumerable<Product> RecentProducts { get; set; } = new List<Product>();
    }
}