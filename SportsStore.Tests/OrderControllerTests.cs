using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using Xunit;

namespace SportsStore.Tests
{
    public class OrderControllerTests
    {
        [Fact]
        public void Cannot_Checkout_Empty_Cart()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var cart = new Cart();
            var controller = new OrderController(mockRepo.Object, cart, NullLogger<OrderController>.Instance);

            var result = controller.Checkout(new Order());

            Assert.False(controller.ModelState.IsValid);
            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Never);
        }

        [Fact]
        public void Cannot_Checkout_Invalid_ShippingDetails()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var cart = new Cart();
            cart.AddItem(new Product { ProductID = 1, Name = "P1" }, 1);

            var controller = new OrderController(mockRepo.Object, cart, NullLogger<OrderController>.Instance);
            controller.ModelState.AddModelError("Name", "Required");

            var result = controller.Checkout(new Order());


            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Never);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void Can_Checkout_And_Submit_Order()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var cart = new Cart();
            cart.AddItem(new Product { ProductID = 1, Name = "P1" }, 1);

            var controller = new OrderController(mockRepo.Object, cart, NullLogger<OrderController>.Instance);

            var result = controller.Checkout(new Order { Name = "Temka" });

            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Once);
            Assert.IsType<RedirectToPageResult>(result);
        }
    }
}