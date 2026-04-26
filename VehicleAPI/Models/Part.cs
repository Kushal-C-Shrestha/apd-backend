using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Part
    {
        [Key]
        public int PartId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public decimal UnitPrice { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
        public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();

        public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    }
}
