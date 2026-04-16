namespace SportsStore.Models.DTOs
{
    public class OrderDto
    {
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public List<OrderItemDto> Items { get; set; } = new();
    }

    public class OrderItemDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}