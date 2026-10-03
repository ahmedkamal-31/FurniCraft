using System.Text.Json;
using FurniCraft.Data;
using FurniCraft.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;

        private const string CartSessionKey = "FurniCraft_Cart";

        public CartService(
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ISession Session =>
            _httpContextAccessor.HttpContext!.Session;

        public CartViewModel GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);

            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            return JsonSerializer.Deserialize<CartViewModel>(cartJson)
                   ?? new CartViewModel();
        }

        public async Task<(bool Success, string? ErrorMessage)> AddToCartAsync(
            int productId,
            List<int> selectedOptionIds,
            int quantity = 1)
        {
            if (quantity <= 0)
            {
                return (false, "الكمية المطلوبة غير صحيحة.");
            }

            var product = await _context.Products
                .Include(p => p.CustomizationGroups)
                .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null)
            {
                return (false, "المنتج غير موجود.");
            }

            if (!product.IsActive)
            {
                return (false, "هذا المنتج غير متاح حالياً.");
            }

            if (product.StockQuantity <= 0)
            {
                return (false, "عفواً، هذا المنتج غير متوفر حالياً.");
            }

            var cart = GetCart();

            /*
             * كل Variants لنفس المنتج تشترك في نفس الـ Stock.
             * لذلك نحسب إجمالي الكمية الموجودة بالفعل في السلة
             * لهذا المنتج مهما اختلفت التخصيصات.
             */
            var existingProductQuantity = cart.Items
                .Where(i => i.ProductId == productId)
                .Sum(i => i.Quantity);

            if (existingProductQuantity + quantity > product.StockQuantity)
            {
                var remaining =
                    product.StockQuantity - existingProductQuantity;

                return (
                    false,
                    $"لا يمكن إضافة هذه الكمية. المتاح لك حالياً {Math.Max(remaining, 0)} قطعة فقط."
                );
            }

            // جلب التخصيصات المختارة
            var selectedOptions = await _context.CustomizationOptions
                .Include(o => o.CustomizationGroup)
                .Where(o => selectedOptionIds.Contains(o.Id))
                .ToListAsync();

            decimal extraPrice =
                selectedOptions.Sum(o => o.AdditionalPrice);

            decimal unitPrice =
                product.BasePrice + extraPrice;

            var customizationDescriptions =
                selectedOptions
                    .Select(o =>
                        $"{o.CustomizationGroup?.Name}: {o.Name}" +
                        (o.AdditionalPrice > 0
                            ? $" (+{o.AdditionalPrice:N0} ج.م)"
                            : ""))
                    .ToList();

            // ترتيب الـ IDs مهم للمقارنة
            var normalizedOptionIds =
                selectedOptionIds
                    .OrderBy(id => id)
                    .ToList();

            // هل نفس المنتج بنفس التخصيصات موجود بالفعل؟
            var existingItem = cart.Items.FirstOrDefault(i =>
                i.ProductId == productId &&
                i.SelectedOptionIds
                    .OrderBy(id => id)
                    .SequenceEqual(normalizedOptionIds));

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Items.Add(new CartItemViewModel
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    ProductImage = product.MainImage ?? string.Empty,
                    BasePrice = product.BasePrice,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    SelectedCustomizations =
                        customizationDescriptions,
                    SelectedOptionIds =
                        normalizedOptionIds
                });
            }

            SaveCart(cart);

            return (true, null);
        }

        public void RemoveFromCart(
            int productId,
            List<int> selectedOptionIds)
        {
            var cart = GetCart();

            var normalizedOptionIds =
                selectedOptionIds
                    .OrderBy(id => id)
                    .ToList();

            cart.Items.RemoveAll(i =>
                i.ProductId == productId &&
                i.SelectedOptionIds
                    .OrderBy(id => id)
                    .SequenceEqual(normalizedOptionIds));

            SaveCart(cart);
        }

        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }

        private void SaveCart(CartViewModel cart)
        {
            Session.SetString(
                CartSessionKey,
                JsonSerializer.Serialize(cart)
            );
        }
    }
}