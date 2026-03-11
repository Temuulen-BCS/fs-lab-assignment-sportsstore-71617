using SportsStore.Models;

namespace SportsStore.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(Order order, Cart cart, HttpRequest request);
        Task<(bool Paid, string Status, string SessionId, string? PaymentIntentId, long? AmountTotal, string? Currency)>
            VerifyCheckoutSessionAsync(string sessionId);
    }
}