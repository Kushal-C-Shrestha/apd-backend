namespace VehicleAPI.DTOs.Response
{
    public class CustomerProfileResponseDTO
    {
        public int UserId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? PhotoUrl { get; set; }
    }
}