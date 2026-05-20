using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.DTOs.Request
{
    public class CreateAppointmentDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int VehicleId { get; set; }

        [Required]
        public DateOnly AppointmentDate { get; set; }

        [Required]
        public TimeOnly AppointmentTime { get; set; }

        [MaxLength(200)]
        public string? ServiceType { get; set; }
    }
}
