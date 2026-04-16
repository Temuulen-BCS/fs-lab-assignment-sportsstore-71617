using SportsStore.Models;

namespace SportsStore.Services
{
    public class InventoryService
    {
        private readonly StoreDbContext _dbContext;
        private readonly ILogger<InventoryService> _logger;

        public InventoryService(StoreDbContext dbContext, ILogger<InventoryService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<InventoryRecord> ValidateAsync(OrderViewDto order, CancellationToken cancellationToken = default)
        {
            var hasItems = order.Items.Count > 0;
            var hasInvalidQuantity = order.Items.Any(x => x.Quantity <= 0);
            var success = hasItems && !hasInvalidQuantity;
            var message = success
                ? $"Inventory validation passed for {order.Items.Count} item(s)."
                : "Inventory validation failed because the order payload was incomplete.";

            var record = new InventoryRecord
            {
                OrderId = order.Id,
                Success = success,
                Message = message,
                CheckedAtUtc = DateTime.UtcNow
            };

            _dbContext.InventoryRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Inventory validation completed. OrderId={OrderId} Success={Success} Message={Message}",
                order.Id,
                success,
                message);

            return record;
        }
    }
}
