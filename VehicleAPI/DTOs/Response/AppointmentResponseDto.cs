namespace VehicleAPI.DTOs.Response
{
    public class AppointmentResponseDto
    {
        public int AppointmentId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public DateTime AppointmentDateTime { get; set; }
        public string? ServiceType { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
