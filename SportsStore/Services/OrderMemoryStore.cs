using SportsStore.Models.DTOs;

namespace SportsStore.Services
{
    public class OrderMemoryStore
    {
        private readonly List<OrderViewDto> _orders = [];
        private int _nextId = 1;
        private readonly object _lock = new();

        public OrderViewDto Add(OrderDto dto)
        {
            lock (_lock)
            {
                var order = new OrderViewDto
                {
                    Id = _nextId++,
                    CustomerName = dto.CustomerName,
                    Address = dto.Address,
                    City = dto.City,
                    Country = dto.Country,
                    Status = "Pending",
                    CreatedAtUtc = DateTime.UtcNow,
                    Items = dto.Items.Select(x => new OrderItemViewDto
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Price = x.Price,
                        Quantity = x.Quantity
                    }).ToList()
                };

                _orders.Add(order);
                return order;
            }
        }

        public List<OrderViewDto> GetAll()
        {
            lock (_lock)
            {
                return _orders
                    .OrderByDescending(x => x.CreatedAtUtc)
                    .ToList();
            }
        }

        public OrderViewDto? GetById(int id)
        {
            lock (_lock)
            {
                return _orders.FirstOrDefault(x => x.Id == id);
            }
        }

        public bool UpdateStatus(int id, string status)
        {
            lock (_lock)
            {
                var order = _orders.FirstOrDefault(x => x.Id == id);
                if (order == null)
                    return false;

                order.Status = status;
                return true;
            }
        }
    }

    public class OrderViewDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string Address { get; set; } = "";
        public string City { get; set; } = "";
        public string Country { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAtUtc { get; set; }
        public List<OrderItemViewDto> Items { get; set; } = [];
    }

    public class OrderItemViewDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }
}