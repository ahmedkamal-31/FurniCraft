using FurniCraft.Data;
using FurniCraft.Models;
using FurniCraft.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Controllers
{
    [Authorize(Roles = "Admin")]
    public class CustomizationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomizationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // عرض تخصيصات منتج معين
        [HttpGet]
        public async Task<IActionResult> Manage(int productId)
        {
            var product = await _context.Products
                .Include(p => p.CustomizationGroups)
                .ThenInclude(g => g.Options)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            return View(product);
        }

        // إضافة مجموعة تخصيص جديدة
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CustomizationGroupCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var group = new CustomizationGroup
                {
                    ProductId = model.ProductId,
                    Name = model.Name
                };

                _context.CustomizationGroups.Add(group);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Manage), new { productId = model.ProductId });
            }

            return RedirectToAction(nameof(Manage), new { productId = model.ProductId });
        }

        // إضافة خيار داخل مجموعة تخصيص
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOption(CustomizationOptionCreateViewModel model, int productId)
        {
            if (ModelState.IsValid)
            {
                var option = new CustomizationOption
                {
                    CustomizationGroupId = model.GroupId,
                    Name = model.Name,
                    AdditionalPrice = model.AdditionalPrice
                };

                _context.CustomizationOptions.Add(option);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Manage), new { productId = productId });
        }

        // حذف خيار تخصيص
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOption(int id, int productId)
        {
            var option = await _context.CustomizationOptions.FindAsync(id);
            if (option != null)
            {
                _context.CustomizationOptions.Remove(option);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Manage), new { productId = productId });
        }

        // حذف مجموعة تخصيص بالكامل
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int id, int productId)
        {
            var group = await _context.CustomizationGroups.FindAsync(id);
            if (group != null)
            {
                _context.CustomizationGroups.Remove(group);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Manage), new { productId = productId });
        }
    }
}
