using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleAPI.Models
{
    public class PurchaseItem
    {
        [Key]
        public int PurchaseItemId { get; set; }

        [Required]
        public int PurchaseId { get; set; }

        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitCost { get; set; }

        public decimal Subtotal { get; set; }

        [ForeignKey("PurchaseId")]
        public Purchase Purchase { get; set; }

        [ForeignKey("PartId")]
        public Part Part { get; set; }
    }
}
