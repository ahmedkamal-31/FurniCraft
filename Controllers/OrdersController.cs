using FurniCraft.Data;
using FurniCraft.Models;
using FurniCraft.Services;
using FurniCraft.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(ApplicationDbContext context, CartService cartService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
        }

        // 1. صفحة إتمام الطلب (Checkout)
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var user = await _userManager.GetUserAsync(User);
            var model = new CheckoutViewModel
            {
                CustomerName = user?.FullName ?? string.Empty,
                Phone = user?.PhoneNumber ?? string.Empty,
                Cart = cart
            };

            return View(model);
        }

        // 2. معالجة الطلب والتخزين
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = _cartService.GetCart();
            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "سلة التسوق فارغة.");
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Cart = cart;
                return View("Checkout", model);
            }

            var userId = _userManager.GetUserId(User)!;

            var order = new Order
            {
                OrderNumber = "ORD-" + DateTime.UtcNow.Ticks.ToString()[^8..],
                UserId = userId,
                CustomerName = model.CustomerName,
                Phone = model.Phone,
                City = model.City,
                Address = model.Address,
                Notes = model.Notes,
                TotalAmount = cart.GrandTotal,
                Status = "Pending",
                PaymentMethod = "CashOnDelivery",
                OrderDate = DateTime.UtcNow
            };

            foreach (var item in cart.Items)
            {
                var orderItem = new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                };

                if (item.SelectedOptionIds != null && item.SelectedOptionIds.Any())
                {
                    var options = await _context.CustomizationOptions
                        .Include(o => o.CustomizationGroup)
                        .Where(o => item.SelectedOptionIds.Contains(o.Id))
                        .ToListAsync();

                    foreach (var opt in options)
                    {
                        orderItem.Customizations.Add(new OrderItemCustomization
                        {
                            GroupName = opt.CustomizationGroup?.Name ?? "تخصيص",
                            OptionName = opt.Name,
                            AdditionalPrice = opt.AdditionalPrice
                        });
                    }
                }

                order.Items.Add(orderItem);
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _cartService.ClearCart();

            return RedirectToAction(nameof(Confirmation), new { id = order.Id });
        }

        // 3. صفحة تأكيد الطلب
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.Customizations)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);

            if (order == null) return NotFound();

            return View(order);
        }

        // 4. عرض طلباتي للعميل
        [HttpGet]
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 5. إدارة الطلبات للـ Admin
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminIndex()
        {
            var orders = await _context.Orders
                .Include(o => o.Items)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // 6. تحديث حالة الطلب من الـ Admin
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = status;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
