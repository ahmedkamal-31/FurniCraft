using System.ComponentModel.DataAnnotations;

namespace FurniCraft.ViewModels
{
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "اسم المنتج مطلوب")]
        [Display(Name = "اسم المنتج")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "الوصف مطلوب")]
        [Display(Name = "الوصف")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "السعر مطلوب")]
        [Range(0.01, 1000000, ErrorMessage = "السعر يجب أن يكون أكبر من 0")]
        [Display(Name = "السعر الأساسي")]
        public decimal BasePrice { get; set; }

        [Required(ErrorMessage = "الكمية المتاحة مطلوبة")]
        [Range(0, 10000, ErrorMessage = "الكمية غير صحيحة")]
        [Display(Name = "الكمية بالخزينة")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "التصنيف مطلوب")]
        [Display(Name = "التصنيف")]
        public int CategoryId { get; set; }

        [Display(Name = "صورة المنتج")]
        public IFormFile? ImageFile { get; set; }
    }
}
