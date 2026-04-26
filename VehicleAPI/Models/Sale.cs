using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Sale
    {
        [Key]
        public int SaleId { get; set; }

        [Required]
        public int UserId { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal Discount { get; set; }

        public decimal FinalAmount { get; set; }

        public decimal AmountPaid { get; set; } 

        public string PaymentStatus { get; set; } 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }

        public ICollection<SaleItem> SaleItems { get; set; }
        public Credit Credit { get; set; } // NEW (optional navigation)

    }
}
