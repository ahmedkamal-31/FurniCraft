using System.ComponentModel.DataAnnotations;
namespace FurniCraft.Models {
    public class Cart {
        public int Id { get; set; }
        [Required] public string UserId { get; set; } = string.Empty;
        public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
    }
}
