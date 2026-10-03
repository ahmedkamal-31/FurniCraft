using FurniCraft.Data;
using FurniCraft.ViewModels;
using FurniCraft.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var totalProducts = await _context.Products.CountAsync();

            var activeProducts = await _context.Products
                .CountAsync(p => p.IsActive);

            var lowStockProducts = await _context.Products
                .CountAsync(p => p.StockQuantity <= 5 && p.IsActive);

            var totalOrders = await _context.Orders.CountAsync();

            var pendingOrders = await _context.Orders
                .CountAsync(o => o.Status == "Pending");

            var deliveredOrders = await _context.Orders
                .CountAsync(o => o.Status == "Delivered");

            var totalRevenue = await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

            var totalUsers = await _userManager.Users.CountAsync();

            var recentOrders = await _context.Orders
                .OrderByDescending(o => o.OrderDate)
                .Take(6)
                .ToListAsync();

            var model = new AdminDashboardViewModel
            {
                TotalProducts = totalProducts,
                ActiveProducts = activeProducts,
                LowStockProducts = lowStockProducts,
                TotalOrders = totalOrders,
                PendingOrders = pendingOrders,
                DeliveredOrders = deliveredOrders,
                TotalRevenue = totalRevenue,
                TotalUsers = totalUsers,
                RecentOrders = recentOrders
            };

            return View(model);
        }
    }
}