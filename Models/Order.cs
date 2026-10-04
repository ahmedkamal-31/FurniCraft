using System.ComponentModel.DataAnnotations;
namespace FurniCraft.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required, MaxLength(50)] public string OrderNumber { get; set; } = string.Empty;
        [Required] public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        [Required, MaxLength(50)] public string Status { get; set; } = "Pending";
        [Required, MaxLength(50)] public string PaymentMethod { get; set; } = "CashOnDelivery";
        [Required, MaxLength(100)] public string CustomerName { get; set; } = string.Empty;
        [Required, MaxLength(20)] public string Phone { get; set; } = string.Empty;
        [Required, MaxLength(100)] public string City { get; set; } = string.Empty;
        [Required, MaxLength(300)] public string Address { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    }
}