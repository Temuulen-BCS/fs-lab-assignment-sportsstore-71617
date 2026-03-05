using Microsoft.AspNetCore.Mvc;
using SportsStore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SportsStore.Controllers;

namespace SportsStore.Controllers
{
    public class OrderController : Controller
    {
        private readonly IOrderRepository repository;
        private readonly Cart cart;
        private readonly ILogger<OrderController> logger;

        public OrderController(IOrderRepository repoService, Cart cartService, ILogger<OrderController> logger)
        {
            repository = repoService;
            cart = cartService;
            this.logger = logger;
        }

        public ViewResult Checkout()
        {
            logger.LogInformation("Checkout page opened. Items={ItemCount} Total={Total}",
                cart.Lines.Count(), cart.ComputeTotalValue());

            return View(new Order());
        }

        [HttpPost]
        public IActionResult Checkout(Order order)
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

            logger.LogInformation("Saving order. Customer={Name} Items={ItemCount} Total={Total}",
                order.Name, order.Lines.Count, cart.ComputeTotalValue());

            try
            {
                repository.SaveOrder(order);

                logger.LogInformation("Order saved. OrderId={OrderId}", order.OrderID);

                cart.Clear();

                return RedirectToPage("/Completed", new { orderId = order.OrderID });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Checkout failed while saving order. Customer={Name} Items={ItemCount}",
                    order.Name, order.Lines.Count);

                throw;
            }
        }
    }
}