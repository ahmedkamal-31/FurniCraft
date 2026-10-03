using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FurniCraft.Models
{
    public class OrderItemCustomization
    {
        public int Id { get; set; }
        
        [Required]
        public int OrderItemId { get; set; }
        
        [ForeignKey("OrderItemId")]
        public OrderItem? OrderItem { get; set; }
        
        [Required]
        public string GroupName { get; set; } = string.Empty;
        
        [Required]
        public string OptionName { get; set; } = string.Empty;
        
        public decimal AdditionalPrice { get; set; }
    }
}
