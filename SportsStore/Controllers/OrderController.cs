using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SportsStore.Models;
using SportsStore.Services;

namespace SportsStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository repository;
        private readonly Cart cart;
        private readonly ILogger<OrderController> logger;
        private readonly IPaymentService paymentService;

        private const string PendingOrderSessionKey = "PendingOrder";

        public OrderController(
            IOrderRepository repoService,
            Cart cartService,
            ILogger<OrderController> logger,
            IPaymentService paymentService)
        {
            repository = repoService;
            cart = cartService;
            this.logger = logger;
            this.paymentService = paymentService;
        }

        public ViewResult Checkout()
        {
            logger.LogInformation("Checkout page opened. Items={ItemCount} Total={Total}",
                cart.Lines.Count(), cart.ComputeTotalValue());

            return View(new Order());
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            logger.LogInformation("Checkout submit received. Items={ItemCount} Total={Total}",
                cart.Lines.Count(), cart.ComputeTotalValue());

            if (!cart.Lines.Any())
            {
                logger.LogWarning("Checkout blocked: cart empty.");
                ModelState.AddModelError("", "Sorry, your cart is empty!");
            }

            if (!ModelState.IsValid)
            {
                logger.LogWarning("Checkout validation failed. Errors={ErrorCount}", ModelState.ErrorCount);
                return View(order);
            }

            order.Lines = cart.Lines.ToArray();
            HttpContext.Session.SetString(PendingOrderSessionKey, JsonSerializer.Serialize(order));

            try
            {
                var redirectUrl = await paymentService.CreateCheckoutSessionAsync(order, cart, Request);

                logger.LogInformation("Stripe checkout session created. Redirecting to Stripe. Customer={Name} Items={ItemCount}",
                    order.Name, order.Lines.Count);

                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Stripe session creation failed. Customer={Name}", order.Name);
                return RedirectToAction(nameof(PaymentFailed));
            }
        }

        [HttpGet]
        public async Task<IActionResult> PaymentSuccess(string session_id)
        {
            if (string.IsNullOrWhiteSpace(session_id))
                return RedirectToAction(nameof(PaymentFailed));

            var pendingJson = HttpContext.Session.GetString(PendingOrderSessionKey);
            if (string.IsNullOrWhiteSpace(pendingJson))
                return RedirectToAction(nameof(PaymentFailed));

            var order = JsonSerializer.Deserialize<Order>(pendingJson);
            if (order == null)
                return RedirectToAction(nameof(PaymentFailed));

            try
            {
                var verify = await paymentService.VerifyCheckoutSessionAsync(session_id);

                if (!verify.Paid)
                {
                    logger.LogWarning("Stripe payment not paid. Status={Status} SessionId={SessionId}", verify.Status, verify.SessionId);
                    return RedirectToAction(nameof(PaymentFailed));
                }

                order.PaymentStatus = verify.Status;
                order.StripeSessionId = verify.SessionId;
                order.StripePaymentIntentId = verify.PaymentIntentId;
                order.PaymentAmount = verify.AmountTotal;
                order.PaymentCurrency = verify.Currency;
                order.PaidAtUtc = DateTime.UtcNow;

                repository.SaveOrder(order);

                logger.LogInformation("Order saved after payment. OrderId={OrderId} SessionId={SessionId}",
                    order.OrderID, verify.SessionId);

                cart.Clear();
                HttpContext.Session.Remove(PendingOrderSessionKey);

                return RedirectToPage("/Completed", new { orderId = order.OrderID });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Payment success verification failed. SessionId={SessionId}", session_id);
                return RedirectToAction(nameof(PaymentFailed));
            }
        }

        [HttpGet]
        public IActionResult PaymentCancelled()
        {
            logger.LogWarning("Stripe checkout cancelled by user.");
            HttpContext.Session.Remove(PendingOrderSessionKey);
            return View();
        }

        [HttpGet]
        public IActionResult PaymentFailed()
        {
            logger.LogWarning("Stripe payment failed.");
            return View();
        }
    }
}