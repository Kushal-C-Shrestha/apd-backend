namespace VehicleAPI.DTOs.Response
{
    public class LoginResponseDTO
    {
        public int UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }
}