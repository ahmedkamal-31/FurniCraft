using FurniCraft.Services;
using Microsoft.AspNetCore.Mvc;

namespace FurniCraft.Controllers
{
    public class CartController : Controller
    {
        private readonly CartService _cartService;

        public CartController(CartService cartService)
        {
            _cartService = cartService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var cart = _cartService.GetCart();
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, List<int> selectedOptionIds)
        {
            await _cartService.AddToCartAsync(productId, selectedOptionIds ?? new List<int>());
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult RemoveFromCart(int productId, string optionIdsCsv)
        {
            var optionIds = string.IsNullOrEmpty(optionIdsCsv) 
                ? new List<int>() 
                : optionIdsCsv.Split(',').Select(int.Parse).ToList();

            _cartService.RemoveFromCart(productId, optionIds);
            return RedirectToAction(nameof(Index));
        }
    }
}
