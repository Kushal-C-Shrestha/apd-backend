using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required, MaxLength(20)]
        public string VehicleNumber { get; set; } // e.g. BA-1234

        [Required, MaxLength(50)]
        public string Brand { get; set; } 

        [Required, MaxLength(50)]
        public string Model { get; set; } 

        public int Year { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }

    }
}
