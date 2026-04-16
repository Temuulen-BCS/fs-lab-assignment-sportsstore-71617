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
                    PaymentStatus = "Pending",
                    InventoryStatus = "Pending",
                    ShippingStatus = "Pending",
                    CreatedAtUtc = DateTime.UtcNow,
                    Items = dto.Items.Select(x => new OrderItemViewDto
                    {
                        ProductId = x.ProductId,
                        ProductName = x.ProductName,
                        Price = x.Price,
                        Quantity = x.Quantity
                    }).ToList()
                };

                order.ServiceLogs.Add(new ServiceLogViewDto
                {
                    Service = "Overview",
                    Level = "Information",
                    Message = "Order accepted and queued for workflow processing.",
                    TimestampUtc = order.CreatedAtUtc
                });

                _orders.Add(order);
                return order;
            }
        }

        public OrderViewDto Upsert(OrderWorkflowMessage message)
        {
            lock (_lock)
            {
                var order = _orders.FirstOrDefault(x => x.Id == message.OrderId);
                if (order == null)
                {
                    order = new OrderViewDto
                    {
                        Id = message.OrderId > 0 ? message.OrderId : _nextId++,
                        CustomerName = message.CustomerName,
                        Address = message.Address,
                        City = message.City,
                        Country = message.Country,
                        Status = "Submitted",
                        PaymentStatus = message.PaymentStatus ?? "Pending",
                        InventoryStatus = "Pending",
                        ShippingStatus = "Pending",
                        CreatedAtUtc = message.CreatedAtUtc == default ? DateTime.UtcNow : message.CreatedAtUtc,
                        Items = (message.Items ?? [])
                            .Select(x => new OrderItemViewDto
                            {
                                ProductId = x.ProductId,
                                ProductName = x.ProductName,
                                Price = x.Price,
                                Quantity = x.Quantity
                            }).ToList()
                    };

                    _orders.Add(order);
                }
                else
                {
                    order.CustomerName = string.IsNullOrWhiteSpace(message.CustomerName) ? order.CustomerName : message.CustomerName;
                    order.Address = string.IsNullOrWhiteSpace(message.Address) ? order.Address : message.Address;
                    order.City = string.IsNullOrWhiteSpace(message.City) ? order.City : message.City;
                    order.Country = string.IsNullOrWhiteSpace(message.Country) ? order.Country : message.Country;

                    if (message.Items is { Count: > 0 })
                    {
                        order.Items = message.Items.Select(x => new OrderItemViewDto
                        {
                            ProductId = x.ProductId,
                            ProductName = x.ProductName,
                            Price = x.Price,
                            Quantity = x.Quantity
                        }).ToList();
                    }

                    if (!string.IsNullOrWhiteSpace(message.PaymentStatus))
                    {
                        order.PaymentStatus = message.PaymentStatus;
                    }
                }

                _nextId = Math.Max(_nextId, order.Id + 1);
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

        public bool UpdateWorkflowState(
            int id,
            string? status = null,
            string? inventoryStatus = null,
            string? paymentStatus = null,
            string? shippingStatus = null,
            string? shipmentReference = null)
        {
            lock (_lock)
            {
                var order = _orders.FirstOrDefault(x => x.Id == id);
                if (order == null)
                    return false;

                if (!string.IsNullOrWhiteSpace(status))
                    order.Status = status;

                if (!string.IsNullOrWhiteSpace(inventoryStatus))
                    order.InventoryStatus = inventoryStatus;

                if (!string.IsNullOrWhiteSpace(paymentStatus))
                    order.PaymentStatus = paymentStatus;

                if (!string.IsNullOrWhiteSpace(shippingStatus))
                    order.ShippingStatus = shippingStatus;

                if (!string.IsNullOrWhiteSpace(shipmentReference))
                    order.ShipmentReference = shipmentReference;

                return true;
            }
        }

        public bool AddLog(int id, string service, string message, string level = "Information")
        {
            lock (_lock)
            {
                var order = _orders.FirstOrDefault(x => x.Id == id);
                if (order == null)
                    return false;

                order.ServiceLogs.Add(new ServiceLogViewDto
                {
                    Service = service,
                    Level = level,
                    Message = message,
                    TimestampUtc = DateTime.UtcNow
                });

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
        public string PaymentStatus { get; set; } = "";
        public string InventoryStatus { get; set; } = "";
        public string ShippingStatus { get; set; } = "";
        public string? ShipmentReference { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public List<OrderItemViewDto> Items { get; set; } = [];
        public List<ServiceLogViewDto> ServiceLogs { get; set; } = [];
    }

    public class OrderItemViewDto
    {
        public long ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    public class ServiceLogViewDto
    {
        public string Service { get; set; } = "";
        public string Level { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime TimestampUtc { get; set; }
    }
}
