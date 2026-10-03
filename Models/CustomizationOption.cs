using System.ComponentModel.DataAnnotations;
namespace FurniCraft.Models {
    public class CustomizationOption {
        public int Id { get; set; }
        [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
        public decimal AdditionalPrice { get; set; }
        public int CustomizationGroupId { get; set; }
        public CustomizationGroup? CustomizationGroup { get; set; }
    }
}
