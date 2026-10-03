using System.ComponentModel.DataAnnotations;
namespace FurniCraft.Models {
    public class Product {
        public int Id { get; set; }
        [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal BasePrice { get; set; }
        public int StockQuantity { get; set; }
        public string? MainImage { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<CustomizationGroup> CustomizationGroups { get; set; } = new List<CustomizationGroup>();
    }
}
