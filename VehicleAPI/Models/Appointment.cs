using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Appointment
    {
        [Key]
        public int AppointmentId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public DateTime AppointmentDateTime { get; set; }

        [MaxLength(200)]
        public string? ServiceType { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User? User { get; set; }
        public Vehicle? Vehicle { get; set; }
    }
}
