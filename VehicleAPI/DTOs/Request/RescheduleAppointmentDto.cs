using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.DTOs.Request
{
    public class RescheduleAppointmentDto
    {
        [Required]
        public DateOnly AppointmentDate { get; set; }

        [Required]
        public TimeOnly AppointmentTime { get; set; }

        public string ServiceType { get; set; } = string.Empty;

        public int? VehicleId { get; set; }
    }
}
