using FurniCraft.Models;

namespace FurniCraft.ViewModels
{
    public class HomeViewModel
    {
        public List<Category> Categories { get; set; } = new();

        public List<Product> FeaturedProducts { get; set; } = new();
    }
}