namespace SportsStore.Models.DTOs
{
    public class OrderWorkflowMessage
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public string? PaymentStatus { get; set; }
        public string? CorrelationId { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<OrderWorkflowItemMessage> Items { get; set; } = [];
    }

    public class OrderWorkflowItemMessage
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}
