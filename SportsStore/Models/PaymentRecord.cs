using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsStore.Models
{
    public class PaymentRecord
    {
        [Key]
        public int PaymentRecordId { get; set; }

        public int OrderId { get; set; }

        public bool Success { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? TransactionReference { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime ProcessedAtUtc { get; set; } = DateTime.UtcNow;
    }
}