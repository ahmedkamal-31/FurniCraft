using FurniCraft.Data;
using FurniCraft.Models;
using FurniCraft.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace FurniCraft.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // ==========================================
        // Customer - عرض المنتجات
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Index(
            int? categoryId,
            string? searchTerm,
            string sortBy = "default")
        {
            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                query = query.Where(
                    p => p.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Name.Contains(searchTerm) ||
                    (p.Description != null &&
                     p.Description.Contains(searchTerm)));
            }

            query = sortBy switch
            {
                "price_asc" =>
                    query.OrderBy(p => p.BasePrice),

                "price_desc" =>
                    query.OrderByDescending(p => p.BasePrice),

                "newest" =>
                    query.OrderByDescending(p => p.CreatedAt),

                _ =>
                    query.OrderByDescending(p => p.Id)
            };

            var viewModel = new ProductFilterViewModel
            {
                Products = await query.ToListAsync(),
                Categories = await _context.Categories.ToListAsync(),
                SelectedCategoryId = categoryId,
                SearchTerm = searchTerm,
                SortBy = sortBy
            };

            return View(viewModel);
        }

        // ==========================================
        // Customer - تفاصيل المنتج
        // ==========================================

        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }

        // ==========================================
        // Admin - قائمة المنتجات
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> AdminIndex()
        {
            var products = await _context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.Id)
                .ToListAsync();

            return View(products);
        }

        // ==========================================
        // Admin - إضافة منتج
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.ToListAsync(),
                "Id",
                "Name");

            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProductCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                string imageUrl =
                    "/images/products/default.jpg";

                if (model.ImageFile != null &&
                    model.ImageFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(
                        _webHostEnvironment.WebRootPath,
                        "images",
                        "products");

                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName =
                        Guid.NewGuid().ToString() +
                        "_" +
                        Path.GetFileName(
                            model.ImageFile.FileName);

                    string filePath = Path.Combine(
                        uploadsFolder,
                        uniqueFileName);

                    using var fileStream =
                        new FileStream(
                            filePath,
                            FileMode.Create);

                    await model.ImageFile.CopyToAsync(
                        fileStream);

                    imageUrl =
                        "/images/products/" +
                        uniqueFileName;
                }

                var product = new Product
                {
                    Name = model.Name,
                    Description = model.Description,
                    BasePrice = model.BasePrice,
                    StockQuantity = model.StockQuantity,
                    CategoryId = model.CategoryId,
                    MainImage = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);

                await _context.SaveChangesAsync();

                return RedirectToAction(
                    nameof(AdminIndex));
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories.ToListAsync(),
                "Id",
                "Name",
                model.CategoryId);

            return View(model);
        }

        // ==========================================
        // Admin - تعديل منتج
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            var model = new ProductCreateViewModel
            {
                Name = product.Name,
                Description = product.Description,
                BasePrice = product.BasePrice,
                StockQuantity = product.StockQuantity,
                CategoryId = product.CategoryId
            };

            ViewBag.Categories = new SelectList(
                await _context.Categories.ToListAsync(),
                "Id",
                "Name",
                product.CategoryId);

            ViewBag.CurrentImage =
                product.MainImage;

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            ProductCreateViewModel model)
        {
            var product = await _context.Products
                .FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(
                    await _context.Categories.ToListAsync(),
                    "Id",
                    "Name",
                    model.CategoryId);

                ViewBag.CurrentImage =
                    product.MainImage;

                return View(model);
            }

            product.Name = model.Name;
            product.Description = model.Description;
            product.BasePrice = model.BasePrice;
            product.StockQuantity = model.StockQuantity;
            product.CategoryId = model.CategoryId;

            // تغيير الصورة إذا تم رفع صورة جديدة
            if (model.ImageFile != null &&
                model.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(
                    _webHostEnvironment.WebRootPath,
                    "images",
                    "products");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(
                        uploadsFolder);
                }

                string uniqueFileName =
                    Guid.NewGuid().ToString() +
                    "_" +
                    Path.GetFileName(
                        model.ImageFile.FileName);

                string filePath = Path.Combine(
                    uploadsFolder,
                    uniqueFileName);

                using var fileStream =
                    new FileStream(
                        filePath,
                        FileMode.Create);

                await model.ImageFile.CopyToAsync(
                    fileStream);

                product.MainImage =
                    "/images/products/" +
                    uniqueFileName;
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(
                nameof(AdminIndex));
        }

        // ==========================================
        // Admin - حذف منتج
        // ==========================================

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products
                .FindAsync(id);

            if (product != null)
            {
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();
            }

            return RedirectToAction(
                nameof(AdminIndex));
        }
    }
}