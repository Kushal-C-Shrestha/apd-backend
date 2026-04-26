using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();

    }
}
