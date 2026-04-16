using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SportsStore.Models;
using SportsStore.Models.DTOs;
using SportsStore.Services;
using SportsStore.Services.Messaging;

namespace SportsStore.Controllers
{
    [ApiController]
    public class OrderController : Controller
    {
        private readonly IOrderRepository repository;
        private readonly Cart cart;
        private readonly ILogger<OrderController> logger;
        private readonly IPaymentService paymentService;
        private readonly RabbitMQService _rabbitMQService;
        private readonly OrderMemoryStore _orderMemoryStore;
        private readonly IMapper _mapper;

        private const string PendingOrderSessionKey = "PendingOrder";

        public OrderController(
            IOrderRepository repoService,
            Cart cartService,
            ILogger<OrderController> logger,
            IPaymentService paymentService,
            RabbitMQService rabbitMQService,
            OrderMemoryStore orderMemoryStore,
            IMapper mapper)
        {
            repository = repoService;
            cart = cartService;
            this.logger = logger;
            this.paymentService = paymentService;
            _rabbitMQService = rabbitMQService;
            _orderMemoryStore = orderMemoryStore;
            _mapper = mapper;
        }

        [HttpGet("/Order/Checkout")]
        public ViewResult Checkout()
        {
            logger.LogInformation("Checkout page opened. Items={ItemCount} Total={Total}",
                cart.Lines.Count(), cart.ComputeTotalValue());

            return View(new Order());
        }

        [HttpPost("/Order/Checkout")]
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

        [HttpGet("/Order/PaymentSuccess")]
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

                order.Status = OrderStatus.Submitted;
                order.CreatedAtUtc = DateTime.UtcNow;
                order.UpdatedAtUtc = DateTime.UtcNow;
                order.CorrelationId = Guid.NewGuid().ToString();

                repository.SaveOrder(order);

                logger.LogInformation("Order saved after payment. OrderId={OrderId} SessionId={SessionId}",
                    order.OrderID, verify.SessionId);

                var workflowMessage = BuildWorkflowMessage(order);
                _orderMemoryStore.Upsert(workflowMessage);
                _orderMemoryStore.AddLog(order.OrderID, "Overview", "Order saved after successful payment and queued for workflow processing.");

                var message = JsonSerializer.Serialize(workflowMessage);
                _rabbitMQService.SendMessage("orderQueue", message);

                logger.LogInformation("RabbitMQ message sent for OrderId={OrderId} Queue={Queue}",
                    order.OrderID, "orderQueue");

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

        [HttpGet("/api/orders/{id}")]
        public IActionResult GetOrderById(int id)
        {
            var order = _orderMemoryStore.GetById(id);

            if (order == null)
                return NotFound();

            return Ok(order);
        }

        [HttpGet("/Order/PaymentCancelled")]
        public IActionResult PaymentCancelled()
        {
            logger.LogWarning("Stripe checkout cancelled by user.");
            HttpContext.Session.Remove(PendingOrderSessionKey);
            return View();
        }

        [HttpGet("/Order/PaymentFailed")]
        public IActionResult PaymentFailed()
        {
            logger.LogWarning("Stripe payment failed.");
            return View();
        }

        [HttpPost("/api/orders")]
        public IActionResult CreateOrder([FromBody] OrderDto dto)
        {
            if (dto == null || dto.Items == null || dto.Items.Count == 0)
            {
                logger.LogWarning("API order rejected: no items.");
                return BadRequest("Order has no items.");
            }

            var savedOrder = _orderMemoryStore.Add(dto);
            var workflowMessage = _mapper.Map<OrderWorkflowMessage>(savedOrder);
            var message = JsonSerializer.Serialize(workflowMessage);

            _rabbitMQService.SendMessage("orderQueue", message);
            _orderMemoryStore.AddLog(savedOrder.Id, "Overview", "Workflow message published to RabbitMQ.");

            logger.LogInformation("AutoMapper used to map OrderViewDto to OrderWorkflowMessage. OrderId={OrderId}", savedOrder.Id);
            logger.LogInformation("Blazor order created and queued. OrderId={OrderId}", savedOrder.Id);

            return Ok(savedOrder);
        }

        [HttpGet("/api/orders")]
        public IActionResult GetOrders()
        {
            var orders = _orderMemoryStore.GetAll();
            return Ok(orders);
        }

        private static OrderWorkflowMessage BuildWorkflowMessage(Order order)
        {
            var items = order.Items.Any()
                ? order.Items.Select(x => new OrderWorkflowItemMessage
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Price = x.UnitPrice,
                    Quantity = x.Quantity
                }).ToList()
                : order.Lines.Select(x => new OrderWorkflowItemMessage
                {
                    ProductId = x.Product.ProductID ?? 0,
                    ProductName = x.Product.Name,
                    Price = x.Product.Price,
                    Quantity = x.Quantity
                }).ToList();

            return new OrderWorkflowMessage
            {
                OrderId = order.OrderID,
                CustomerName = order.Name ?? string.Empty,
                Address = order.Line1 ?? string.Empty,
                City = order.City ?? string.Empty,
                Country = order.Country ?? string.Empty,
                PaymentStatus = order.PaymentStatus,
                CorrelationId = order.CorrelationId,
                CreatedAtUtc = order.CreatedAtUtc,
                Items = items
            };
        }

        private static OrderWorkflowMessage BuildWorkflowMessage(OrderViewDto order)
        {
            return new OrderWorkflowMessage
            {
                OrderId = order.Id,
                CustomerName = order.CustomerName,
                Address = order.Address,
                City = order.City,
                Country = order.Country,
                PaymentStatus = order.PaymentStatus,
                CreatedAtUtc = order.CreatedAtUtc,
                Items = order.Items.Select(x => new OrderWorkflowItemMessage
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    Price = x.Price,
                    Quantity = x.Quantity
                }).ToList()
            };
        }
    }
}