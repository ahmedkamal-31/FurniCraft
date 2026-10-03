using FurniCraft.Data;
using FurniCraft.Models;
using FurniCraft.Services;
using FurniCraft.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace FurniCraft.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly CartService _cartService;
        private readonly UserManager<ApplicationUser> _userManager;

        public OrdersController(
            ApplicationDbContext context,
            CartService cartService,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
        }

        // 1. ???? ????? ?????
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

        // 2. ?????? ????? ???? ???????
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = _cartService.GetCart();

            if (!cart.Items.Any())
            {
                ModelState.AddModelError("", "??? ?????? ?????.");
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Cart = cart;
                return View("Checkout", model);
            }

            /*
             * ???? Transaction ????:
             * ????? ????? + ??? ???????
             * ?????? ?? ???.
             */
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            try
            {
                /*
                 * ????? ?????? ???????? ??? ????.
                 *
                 * ??? ???? ??? ??? ?????? ???? ???? ?????
                 * ?? ????? ????? ?? customization.
                 */
                var requestedQuantities = cart.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(i => i.Quantity));

                var productIds = requestedQuantities.Keys.ToList();

                /*
                 * ??? ???????? ?? ????? ????????
                 * ???? ???????? ??? ?????? ??? Session.
                 */
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                /*
                 * ?????? ?? ???? ???????? ???? Stock.
                 */
                foreach (var requested in requestedQuantities)
                {
                    if (!products.TryGetValue(
                        requested.Key,
                        out var product))
                    {
                        ModelState.AddModelError(
                            "",
                            "??? ???????? ???????? ?? ????? ?? ??? ???????.");

                        await transaction.RollbackAsync();

                        model.Cart = cart;
                        return View("Checkout", model);
                    }

                    if (!product.IsActive)
                    {
                        ModelState.AddModelError(
                            "",
                            $"?????? \"{product.Name}\" ?? ??? ??????.");

                        await transaction.RollbackAsync();

                        model.Cart = cart;
                        return View("Checkout", model);
                    }

                    if (requested.Value > product.StockQuantity)
                    {
                        ModelState.AddModelError(
                            "",
                            $"?????? \"{product.Name}\" ???? ??? {product.StockQuantity} ???? ???? ????? ???? {requested.Value}.");

                        await transaction.RollbackAsync();

                        model.Cart = cart;
                        return View("Checkout", model);
                    }
                }

                var userId = _userManager.GetUserId(User)!;

                /*
                 * ????? TotalAmount ?? ????? ?????????
                 * ???? ?? ????? ?????? ?? Session.
                 */
                decimal calculatedOrderTotal = 0;

                var order = new Order
                {
                    OrderNumber =
                        "ORD-" +
                        DateTime.UtcNow.Ticks.ToString()[^8..],

                    UserId = userId,

                    CustomerName = model.CustomerName,
                    Phone = model.Phone,
                    City = model.City,
                    Address = model.Address,
                    Notes = model.Notes,

                    Status = "Pending",
                    PaymentMethod = "CashOnDelivery",
                    OrderDate = DateTime.UtcNow
                };

                foreach (var item in cart.Items)
                {
                    var product = products[item.ProductId];

                    /*
                     * ????? ??? ??? customization options
                     * ?? ????? ????????.
                     */
                    var selectedOptions = new List<CustomizationOption>();

                    if (item.SelectedOptionIds != null &&
                        item.SelectedOptionIds.Any())
                    {
                        selectedOptions = await _context.CustomizationOptions
                            .Include(o => o.CustomizationGroup)
                            .Where(o =>
                                item.SelectedOptionIds.Contains(o.Id))
                            .ToListAsync();
                    }

                    /*
                     * ???? ??? ?????? ??????? ?? DB.
                     */
                    decimal extraPrice =
                        selectedOptions.Sum(o => o.AdditionalPrice);

                    decimal unitPrice =
                        product.BasePrice + extraPrice;

                    decimal itemTotal =
                        unitPrice * item.Quantity;

                    calculatedOrderTotal += itemTotal;

                    var orderItem = new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        TotalPrice = itemTotal
                    };

                    /*
                     * ??? Snapshot ?? ????????? ???? ?????.
                     */
                    foreach (var option in selectedOptions)
                    {
                        orderItem.Customizations.Add(
                            new OrderItemCustomization
                            {
                                GroupName =
                                    option.CustomizationGroup?.Name
                                    ?? "?????",

                                OptionName = option.Name,

                                AdditionalPrice =
                                    option.AdditionalPrice
                            });
                    }

                    order.Items.Add(orderItem);
                }

                /*
                 * ?????? ????? ??????? ?? DB.
                 */
                order.TotalAmount = calculatedOrderTotal;

                /*
                 * ??? ?????? ?? ??? Stock.
                 */
                foreach (var requested in requestedQuantities)
                {
                    var product = products[requested.Key];

                    product.StockQuantity -= requested.Value;
                }

                /*
                 * ????? ????? + ????? ????????.
                 */
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                /*
                 * ????? ??? Transaction.
                 */
                await transaction.CommitAsync();

                /*
                 * ????? ????? ??? ??? ???? ??????? ???????.
                 */
                _cartService.ClearCart();

                return RedirectToAction(
                    nameof(Confirmation),
                    new { id = order.Id });
            }
            catch
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    "",
                    "??? ??? ????? ????? ?????. ???? ??? ????.");

                model.Cart = cart;

                return View("Checkout", model);
            }
        }

        // 3. ???? ????? ?????
        [HttpGet]
        public async Task<IActionResult> Confirmation(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                .ThenInclude(i => i.Customizations)
                .FirstOrDefaultAsync(
                    o => o.Id == id &&
                         o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // 4. ??? ?????? ??????
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

        // 5. ????? ??????? ??? Admin
        // ==========================================
        // Admin - إدارة الطلبات
        // ==========================================

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

        // ==========================================
        // Admin - تفاصيل الطلب
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Customizations)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // ==========================================
        // Admin - تغيير حالة الطلب
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(
            int orderId,
            string status)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            var allowedStatuses = new[]
            {
                "Pending",
                "Processing",
                "Shipped",
                "Delivered",
                "Cancelled"
            };

            if (!allowedStatuses.Contains(status))
            {
                return BadRequest();
            }

            order.Status = status;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
