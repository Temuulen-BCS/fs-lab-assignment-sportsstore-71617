using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SportsStore.Models
{

    public class Order
    {

        [BindNever]
        public int OrderID { get; set; }

        public string? PaymentStatus { get; set; }
        public string? StripeSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public long? PaymentAmount { get; set; }
        public string? PaymentCurrency { get; set; }
        public DateTime? PaidAtUtc { get; set; }

        public OrderStatus Status { get; set; } = OrderStatus.Submitted;

        public string CorrelationId { get; set; } = Guid.NewGuid().ToString();

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }

        public string? FailureReason { get; set; }

        public string? ShipmentReference { get; set; }

        [BindNever]
        public ICollection<CartLine> Lines { get; set; } = new List<CartLine>();

        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();

        [Required(ErrorMessage = "Please enter a name")]
        public string? Name { get; set; }

        [Required(ErrorMessage = "Please enter the first address line")]
        public string? Line1 { get; set; }
        public string? Line2 { get; set; }
        public string? Line3 { get; set; }

        [Required(ErrorMessage = "Please enter a city name")]
        public string? City { get; set; }

        [Required(ErrorMessage = "Please enter a state name")]
        public string? State { get; set; }

        public string? Zip { get; set; }

        [Required(ErrorMessage = "Please enter a country name")]
        public string? Country { get; set; }

        public bool GiftWrap { get; set; }

        [BindNever]
        public bool Shipped { get; set; }
    }
}