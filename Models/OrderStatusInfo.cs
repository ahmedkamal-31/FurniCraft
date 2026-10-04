namespace FurniCraft.Models
{
    /// <summary>
    /// Display helpers for order statuses (labels, icons, css keys).
    /// Status values in the database stay in English.
    /// </summary>
    public static class OrderStatusInfo
    {
        public static string CustomerLabel(string status) => status switch
        {
            "Pending" => "تم استلام الطلب",
            "Processing" => "جاري تجهيز الطلب",
            "Shipped" => "الطلب في الطريق إليك",
            "Delivered" => "تم تسليم الطلب",
            "Cancelled" => "تم إلغاء الطلب",
            _ => status
        };

        public static string AdminLabel(string status) => status switch
        {
            "Pending" => "قيد الانتظار",
            "Processing" => "قيد التحضير",
            "Shipped" => "تم الشحن",
            "Delivered" => "تم التسليم",
            "Cancelled" => "ملغي",
            _ => status
        };

        public static string Icon(string status) => status switch
        {
            "Pending" => "bi-receipt",
            "Processing" => "bi-box-seam",
            "Shipped" => "bi-truck",
            "Delivered" => "bi-check-circle",
            "Cancelled" => "bi-x-circle",
            _ => "bi-circle"
        };

        public static string Key(string status) => status switch
        {
            "Pending" => "pending",
            "Processing" => "processing",
            "Shipped" => "shipped",
            "Delivered" => "delivered",
            "Cancelled" => "cancelled",
            _ => "other"
        };
    }
}