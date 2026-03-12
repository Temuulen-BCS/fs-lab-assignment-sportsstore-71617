using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SportsStore.Controllers;
using SportsStore.Models;
using SportsStore.Services;
using Xunit;

namespace SportsStore.Tests
{
    public class OrderControllerTests
    {
        [Fact]
        public async Task Cannot_Checkout_Empty_Cart()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var paymentMock = new Mock<IPaymentService>();
            var cart = new Cart();

            var controller = new OrderController(
                mockRepo.Object,
                cart,
                NullLogger<OrderController>.Instance,
                paymentMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    Session = new TestSession()
                }
            };

            var result = await controller.Checkout(new Order());

            Assert.False(controller.ModelState.IsValid);
            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Never);
            paymentMock.Verify(p => p.CreateCheckoutSessionAsync(
                It.IsAny<Order>(),
                It.IsAny<Cart>(),
                It.IsAny<HttpRequest>()), Times.Never);
        }

        [Fact]
        public async Task Cannot_Checkout_Invalid_ShippingDetails()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var paymentMock = new Mock<IPaymentService>();
            var cart = new Cart();
            cart.AddItem(new Product { ProductID = 1, Name = "P1" }, 1);

            var controller = new OrderController(
                mockRepo.Object,
                cart,
                NullLogger<OrderController>.Instance,
                paymentMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    Session = new TestSession()
                }
            };

            controller.ModelState.AddModelError("Name", "Required");

            var result = await controller.Checkout(new Order());

            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Never);
            paymentMock.Verify(p => p.CreateCheckoutSessionAsync(
                It.IsAny<Order>(),
                It.IsAny<Cart>(),
                It.IsAny<HttpRequest>()), Times.Never);

            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Can_Checkout_And_Redirect_To_Stripe()
        {
            var mockRepo = new Mock<IOrderRepository>();
            var paymentMock = new Mock<IPaymentService>();
            var cart = new Cart();
            cart.AddItem(new Product { ProductID = 1, Name = "P1" }, 1);

            paymentMock
                .Setup(p => p.CreateCheckoutSessionAsync(
                    It.IsAny<Order>(),
                    It.IsAny<Cart>(),
                    It.IsAny<HttpRequest>()))
                .ReturnsAsync("https://checkout.stripe.com/test-session");

            var controller = new OrderController(
                mockRepo.Object,
                cart,
                NullLogger<OrderController>.Instance,
                paymentMock.Object);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
                {
                    Session = new TestSession()
                }
            };

            var result = await controller.Checkout(new Order { Name = "Temka" });

            mockRepo.Verify(r => r.SaveOrder(It.IsAny<Order>()), Times.Never);

            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("https://checkout.stripe.com/test-session", redirect.Url);
        }

        private class TestSession : ISession
        {
            private readonly Dictionary<string, byte[]> _sessionStorage = new();

            public IEnumerable<string> Keys => _sessionStorage.Keys;
            public string Id => "TestSession";
            public bool IsAvailable => true;

            public void Clear() => _sessionStorage.Clear();

            public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Remove(string key) => _sessionStorage.Remove(key);

            public void Set(string key, byte[] value) => _sessionStorage[key] = value;

            public bool TryGetValue(string key, out byte[] value) => _sessionStorage.TryGetValue(key, out value);
        }
    }
}