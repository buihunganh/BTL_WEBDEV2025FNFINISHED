namespace BTL_WEBDEV2025.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public string? PaymentToken { get; set; }
        public string Status { get; set; } = "New";
        public string PaymentMethod { get; set; } = string.Empty;
        public string? ShippingAddress { get; set; }
        public string? NotificationEmail { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}
