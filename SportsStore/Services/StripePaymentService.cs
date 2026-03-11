using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using SportsStore.Models;
using Stripe;
using Stripe.Checkout;

namespace SportsStore.Services
{
    public class StripePaymentService : IPaymentService
    {
        private readonly StripeSettings _settings;

        public StripePaymentService(IOptions<StripeSettings> options)
        {
            _settings = options.Value;
            StripeConfiguration.ApiKey = _settings.SecretKey;
        }

        public async Task<string> CreateCheckoutSessionAsync(Order order, Cart cart, HttpRequest request)
        {
            var domain = $"{request.Scheme}://{request.Host}";

            var lineItems = cart.Lines.Select(l => new SessionLineItemOptions
            {
                Quantity = l.Quantity,
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "eur",
                    UnitAmount = (long)(l.Product.Price * 100m),
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = l.Product.Name
                    }
                }
            }).ToList();

            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = domain + "/Order/PaymentSuccess?session_id={CHECKOUT_SESSION_ID}",
                CancelUrl = domain + "/Order/PaymentCancelled"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);

            return session.Url;
        }

        public async Task<(bool Paid, string Status, string SessionId, string? PaymentIntentId, long? AmountTotal, string? Currency)>
            VerifyCheckoutSessionAsync(string sessionId)
        {
            var service = new SessionService();
            var session = await service.GetAsync(sessionId);

            var paid = session.PaymentStatus == "paid";
            return (paid, session.PaymentStatus ?? "unknown", session.Id, session.PaymentIntentId, session.AmountTotal, session.Currency);
        }
    }
}