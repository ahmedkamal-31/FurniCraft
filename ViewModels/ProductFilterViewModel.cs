using FurniCraft.Models;

namespace FurniCraft.ViewModels
{
    public class ProductFilterViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();

        public int? SelectedCategoryId { get; set; }
        public string? SearchTerm { get; set; }
        public string SortBy { get; set; } = "default";
    }
}
