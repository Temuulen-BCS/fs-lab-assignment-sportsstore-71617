using SportsStore.Models;

namespace SportsStore.Services
{
    public class PaymentworkflowService
    {
        private readonly StoreDbContext _dbContext;
        private readonly ILogger<PaymentworkflowService> _logger;

        public PaymentworkflowService(StoreDbContext dbContext, ILogger<PaymentworkflowService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PaymentRecord> ProcessAsync(OrderViewDto order, CancellationToken cancellationToken = default)
        {
            var amount = order.Items.Sum(x => x.Price * x.Quantity);
            var requestedStatus = order.PaymentStatus;
            var success = !string.Equals(requestedStatus, "failed", StringComparison.OrdinalIgnoreCase);
            var normalizedStatus = success &&
                (string.IsNullOrWhiteSpace(requestedStatus) || string.Equals(requestedStatus, "pending", StringComparison.OrdinalIgnoreCase))
                    ? "Approved"
                    : success
                        ? requestedStatus ?? "Approved"
                        : "Failed";
            var message = success
                ? $"Payment outcome recorded as {normalizedStatus}."
                : "Payment outcome recorded as failed.";

            order.PaymentStatus = normalizedStatus;

            var record = new PaymentRecord
            {
                OrderId = order.Id,
                Success = success,
                Amount = amount,
                TransactionReference = $"PAY-{order.Id}-{DateTime.UtcNow:yyyyMMddHHmmss}",
                Message = message,
                ProcessedAtUtc = DateTime.UtcNow
            };

            _dbContext.PaymentRecords.Add(record);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment workflow completed. OrderId={OrderId} Success={Success} Outcome={Outcome} Amount={Amount}",
                order.Id,
                success,
                normalizedStatus,
                amount);

            return record;
        }
    }
}
