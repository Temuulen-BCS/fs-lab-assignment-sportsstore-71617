namespace SportsStore.Models.DTOs
{
    public class StoredOrder
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";

        public List<OrderItemDto> Items { get; set; } = new();

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}