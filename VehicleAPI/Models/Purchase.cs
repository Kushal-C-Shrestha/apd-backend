using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Purchase
    {
        [Key]
        public int PurchaseId { get; set; }

        [Required]
        public int VendorId { get; set; }

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Vendor Vendor { get; set; }

        public ICollection<PurchaseItem> PurchaseItems { get; set; }
    }
}
