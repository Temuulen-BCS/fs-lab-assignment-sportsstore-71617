using System.ComponentModel.DataAnnotations;

namespace SportsStore.Models
{
    public class ShipmentRecord
    {
        [Key]
        public int ShipmentRecordId { get; set; }

        public int OrderId { get; set; }

        public bool Success { get; set; }

        public string? ShipmentReference { get; set; }

        public DateTime? DispatchDateUtc { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}