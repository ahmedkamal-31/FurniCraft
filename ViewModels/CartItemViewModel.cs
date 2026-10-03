namespace FurniCraft.ViewModels
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public decimal UnitPrice { get; set; } // السعر الأساسي + الإضافات
        public int Quantity { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
        
        // خيارات التخصيص المختارة للعرض
        public List<string> SelectedCustomizations { get; set; } = new List<string>();
        public List<int> SelectedOptionIds { get; set; } = new List<int>();
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal GrandTotal => Items.Sum(i => i.TotalPrice);
    }
}
