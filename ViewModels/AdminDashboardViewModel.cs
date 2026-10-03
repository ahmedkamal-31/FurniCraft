using FurniCraft.Models;

namespace FurniCraft.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }

        public int ActiveProducts { get; set; }

        public int LowStockProducts { get; set; }

        public int TotalOrders { get; set; }

        public int PendingOrders { get; set; }

        public int DeliveredOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public int TotalUsers { get; set; }

        public List<Order> RecentOrders { get; set; } = new List<Order>();
    }
}