using SportsStore.Models;

namespace SportsStore.Services
{
    public class ShippingService
    {
        private readonly StoreDbContext _dbContext;
        private readonly ILogger<ShippingService> _logger;

        public ShippingService(StoreDbContext dbContext, ILogger<ShippingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<ShipmentRecord> CreateShipmentAsync(OrderViewDto order, CancellationToken cancellationToken = default)
        {
            var shipmentReference = $"SHP-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}";
            var message = $"Shipping created with reference {shipmentReference}.";

            var record = new ShipmentRecord
            {
                OrderId = order.Id,
                Success = true,
                ShipmentReference = shipmentReference,
                DispatchDateUtc = DateTime.UtcNow.AddDays(1),
                Message = message,
                CreatedAtUtc = DateTime.UtcNow
            };

            _dbContext.ShipmentRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Shipping created. OrderId={OrderId} ShipmentReference={ShipmentReference}",
                order.Id,
                shipmentReference);

            return record;
        }
    }
}
