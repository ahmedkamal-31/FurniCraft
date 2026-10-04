using System.ComponentModel.DataAnnotations;

namespace FurniCraft.Models
{
    public class OrderStatusHistory
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order? Order { get; set; }

        [Required, MaxLength(50)]
        public string Status { get; set; } = string.Empty;

        [MaxLength(300)]
        public string? Note { get; set; }

        // Who made the change (admin e-mail, or "العميل" for the first entry)
        [MaxLength(256)]
        public string? ChangedBy { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}