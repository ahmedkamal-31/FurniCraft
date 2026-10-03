using System.ComponentModel.DataAnnotations;

namespace FurniCraft.ViewModels
{
    public class CustomizationOptionCreateViewModel
    {
        [Required]
        public int GroupId { get; set; }

        [Required(ErrorMessage = "اسم الخيار مطلوب")]
        [Display(Name = "اسم الخيار")]
        public string Name { get; set; } = string.Empty; // مثال: مخمل، جلد طبيعي، زان

        [Required(ErrorMessage = "السعر الإضافي مطلوب")]
        [Display(Name = "السعر الإضافي (ج.م)")]
        public decimal AdditionalPrice { get; set; } = 0;
    }
}
