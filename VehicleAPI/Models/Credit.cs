using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Credit
    {
        [Key]
        public int CreditId { get; set; }

        [Required]
        public int SaleId { get; set; } 

        [Required]
        public decimal AmountDue { get; set; }

        public bool IsPaid { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Sale Sale { get; set; }
    }
}
