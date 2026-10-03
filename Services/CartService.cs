using System.Text.Json;
using FurniCraft.Data;
using FurniCraft.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Services
{
    public class CartService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private const string CartSessionKey = "FurniCraft_Cart";

        public CartService(IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        private ISession Session => _httpContextAccessor.HttpContext!.Session;

        public CartViewModel GetCart()
        {
            var cartJson = Session.GetString(CartSessionKey);
            if (string.IsNullOrEmpty(cartJson))
            {
                return new CartViewModel();
            }

            return JsonSerializer.Deserialize<CartViewModel>(cartJson) ?? new CartViewModel();
        }

        public async Task AddToCartAsync(int productId, List<int> selectedOptionIds, int quantity = 1)
        {
            var product = await _context.Products
                .Include(p => p.CustomizationGroups)
                .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return;

            var cart = GetCart();

            // جلب تفاصيل الخيارات المختارة لحساب السعر ورسائل الوصف
            var selectedOptions = await _context.CustomizationOptions
                .Include(o => o.CustomizationGroup)
                .Where(o => selectedOptionIds.Contains(o.Id))
                .ToListAsync();

            decimal extraPrice = selectedOptions.Sum(o => o.AdditionalPrice);
            decimal unitPrice = product.BasePrice + extraPrice;

            var customizationDescriptions = selectedOptions
                .Select(o => $"{o.CustomizationGroup?.Name}: {o.Name} ({(o.AdditionalPrice > 0 ? $"+{o.AdditionalPrice:N0} ج.م" : "")})")
                .ToList();

            // التحقق مما إذا كان نفس المنتج ونفس التخصيصات موجوداً بالفعل
            var existingItem = cart.Items.FirstOrDefault(i => 
                i.ProductId == productId && 
                i.SelectedOptionIds.SequenceEqual(selectedOptionIds.OrderBy(id => id)));

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
                    ProductImage = product.MainImage,
                    BasePrice = product.BasePrice,
                    UnitPrice = unitPrice,
                    Quantity = quantity,
                    SelectedCustomizations = customizationDescriptions,
                    SelectedOptionIds = selectedOptionIds.OrderBy(id => id).ToList()
                });
            }

            SaveCart(cart);
        }

        public void RemoveFromCart(int productId, List<int> selectedOptionIds)
        {
            var cart = GetCart();
            cart.Items.RemoveAll(i => i.ProductId == productId && i.SelectedOptionIds.SequenceEqual(selectedOptionIds.OrderBy(id => id)));
            SaveCart(cart);
        }

        public void ClearCart()
        {
            Session.Remove(CartSessionKey);
        }

        private void SaveCart(CartViewModel cart)
        {
            Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
        }
    }
}
