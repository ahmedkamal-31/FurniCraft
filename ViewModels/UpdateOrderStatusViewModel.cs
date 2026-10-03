namespace FurniCraft.ViewModels
{
    public class UpdateOrderStatusViewModel
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
