using System.ComponentModel.DataAnnotations;
namespace FurniCraft.Models {
    public class CustomizationGroup {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public Product? Product { get; set; }
        public ICollection<CustomizationOption> Options { get; set; } = new List<CustomizationOption>();
    }
}
