using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class SaleItem
    {
        [Key]
        public int SaleItemId { get; set; }

        [Required]
        public int SaleId { get; set; }

        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal UnitPrice { get; set; } 

        public decimal Subtotal { get; set; }

        public Sale Sale { get; set; }

        public Part Part { get; set; }

    }
}
