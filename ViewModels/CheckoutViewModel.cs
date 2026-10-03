using System.ComponentModel.DataAnnotations;

namespace FurniCraft.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "الاسم بالكامل مطلوب")]
        [Display(Name = "الاسم بالكامل")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رقم الهاتف مطلوب")]
        [Phone(ErrorMessage = "رقم الهاتف غير صحيح")]
        [Display(Name = "رقم الهاتف")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "المدينة / المحافظة مطلوبة")]
        [Display(Name = "المدينة / المحافظة")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "العنوان التفصيلي مطلوب")]
        [Display(Name = "العنوان التفصيلي")]
        public string Address { get; set; } = string.Empty;

        [Display(Name = "ملاحظات إضافية على الطلب")]
        public string? Notes { get; set; }

        public CartViewModel Cart { get; set; } = new CartViewModel();
    }
}
