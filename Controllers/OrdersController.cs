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
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(
            ApplicationDbContext context,
            CartService cartService,
            UserManager<ApplicationUser> userManager,
            ILogger<OrdersController> logger)
        {
            _context = context;
            _cartService = cartService;
            _userManager = userManager;
            _logger = logger;
        }

        // 1. صفحة إتمام الطلب
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

        // 2. تنفيذ الطلب وحفظه في قاعدة البيانات
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PlaceOrder(CheckoutViewModel model)
        {
            var cart = _cartService.GetCart();

            if (!cart.Items.Any())
            {
                // رسالة السلة تظهر في صفحة السلة (TempData["CartError"])
                TempData["CartError"] = "سلة التسوق فارغة.";
                return RedirectToAction("Index", "Cart");
            }

            if (!ModelState.IsValid)
            {
                model.Cart = cart;
                return View("Checkout", model);
            }

            /*
             * نستخدم Transaction واحد:
             * إنشاء الطلب + خصم المخزون
             * يتم معاً أو لا يتم.
             */
            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    IsolationLevel.Serializable);

            // يضيف رسالة خطأ ظاهرة في صفحة Checkout ويلغي العملية
            async Task<IActionResult> RejectAsync(string message)
            {
                ModelState.AddModelError(string.Empty, message);

                await transaction.RollbackAsync();

                model.Cart = cart;
                return View("Checkout", model);
            }

            try
            {
                /*
                 * تجميع الكميات المطلوبة لكل منتج.
                 *
                 * نفس المنتج قد يظهر أكثر من مرة في السلة
                 * بتخصيصات مختلفة، لكنه يشترك في نفس المخزون.
                 */
                var requestedQuantities = cart.Items
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Sum(i => i.Quantity));

                var productIds = requestedQuantities.Keys.ToList();

                /*
                 * نجلب المنتجات من قاعدة البيانات
                 * ولا نعتمد على بيانات الـ Session.
                 */
                var products = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                /*
                 * التحقق من وجود المنتجات وكفاية المخزون.
                 */
                foreach (var requested in requestedQuantities)
                {
                    if (!products.TryGetValue(
                        requested.Key,
                        out var product))
                    {
                        return await RejectAsync(
                            "أحد المنتجات الموجودة في السلة لم يعد متوفراً في المتجر.");
                    }

                    if (!product.IsActive)
                    {
                        return await RejectAsync(
                            $"المنتج \"{product.Name}\" غير متاح حالياً.");
                    }

                    if (requested.Value > product.StockQuantity)
                    {
                        return await RejectAsync(
                            $"المنتج \"{product.Name}\" متوفر منه {product.StockQuantity} قطعة فقط، بينما الكمية المطلوبة {requested.Value}.");
                    }
                }

                var userId = _userManager.GetUserId(User)!;

                /*
                 * حساب TotalAmount من قاعدة البيانات
                 * وليس من الأسعار المخزنة في الـ Session.
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
                     * التحقق من التخصيصات مرة أخرى من قاعدة البيانات:
                     * كل خيار يجب أن يتبع هذا المنتج، وخيار واحد لكل مجموعة،
                     * وكل المجموعات المطلوبة لها اختيار.
                     * (قد تتغير التخصيصات بعد إضافة المنتج للسلة)
                     */
                    var selection =
                        await CustomizationSelectionValidator.ValidateAsync(
                            _context,
                            product.Id,
                            item.SelectedOptionIds);

                    if (!selection.IsValid)
                    {
                        return await RejectAsync(
                            $"تخصيصات المنتج \"{product.Name}\" لم تعد صالحة: {selection.ErrorMessage} " +
                            "يرجى حذفه من السلة وإضافته من جديد.");
                    }

                    /*
                     * نحسب السعر النهائي بالأسعار الحالية في DB.
                     */
                    decimal unitPrice =
                        product.BasePrice + selection.ExtraPrice;

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
                     * حفظ Snapshot من التخصيصات داخل الطلب.
                     */
                    foreach (var option in selection.Options)
                    {
                        orderItem.Customizations.Add(
                            new OrderItemCustomization
                            {
                                GroupName = option.GroupName,

                                OptionName = option.OptionName,

                                AdditionalPrice =
                                    option.AdditionalPrice
                            });
                    }

                    order.Items.Add(orderItem);
                }

                /*
                 * الإجمالي النهائي من DB.
                 */
                order.TotalAmount = calculatedOrderTotal;

                /*
                 * خصم الكميات من المخزون.
                 */
                foreach (var requested in requestedQuantities)
                {
                    var product = products[requested.Key];

                    product.StockQuantity -= requested.Value;
                }

                /*
                 * حفظ الطلب + تعديل المخزون.
                 */
                _context.Orders.Add(order);

                await _context.SaveChangesAsync();

                /*
                 * تأكيد الـ Transaction.
                 */
                await transaction.CommitAsync();

                /*
                 * تفريغ السلة بعد نجاح الطلب فقط.
                 */
                _cartService.ClearCart();

                return RedirectToAction(
                    nameof(Confirmation),
                    new { id = order.Id });
            }
            catch (Exception ex)
            {
                // نسجل الخطأ الحقيقي في الـ Log، ولا نعرض تفاصيله للعميل
                _logger.LogError(
                    ex,
                    "Failed to place order for user {UserId}.",
                    _userManager.GetUserId(User));

                try
                {
                    await transaction.RollbackAsync();
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(
                        rollbackEx,
                        "Transaction rollback failed after an order error.");
                }

                ModelState.AddModelError(
                    string.Empty,
                    "حدث خطأ أثناء إتمام الطلب. يرجى المحاولة مرة أخرى.");

                model.Cart = cart;

                return View("Checkout", model);
            }
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
                .FirstOrDefaultAsync(
                    o => o.Id == id &&
                         o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // 4. طلباتي (للعميل)
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





        // ==========================================
        // Customer - تفاصيل طلب العميل
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> MyOrderDetails(int id)
        {
            var userId = _userManager.GetUserId(User);

            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.Product)
                .Include(o => o.Items)
                    .ThenInclude(i => i.Customizations)
                .FirstOrDefaultAsync(o =>
                    o.Id == id &&
                    o.UserId == userId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }





        // 5. إدارة الطلبات للـ Admin
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