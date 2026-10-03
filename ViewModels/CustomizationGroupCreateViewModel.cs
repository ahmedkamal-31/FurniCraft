using System.ComponentModel.DataAnnotations;

namespace FurniCraft.ViewModels
{
    public class CustomizationGroupCreateViewModel
    {
        [Required(ErrorMessage = "اختر المنتج")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "اسم المجموعة مطلوب")]
        [Display(Name = "اسم مجموعة التخصيص")]
        public string Name { get; set; } = string.Empty; // مثال: نوع القماش، خشب الهيكل
    }
}
