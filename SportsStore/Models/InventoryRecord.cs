using System.ComponentModel.DataAnnotations;

namespace SportsStore.Models
{
    public class InventoryRecord
    {
        [Key]
        public int InventoryRecordId { get; set; }

        public int OrderId { get; set; }

        public bool Success { get; set; }

        public string Message { get; set; } = string.Empty;

        public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    }
}