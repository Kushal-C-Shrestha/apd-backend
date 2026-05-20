namespace VehicleAPI.DTOs.Request
{
    public class SaveVehicleDTO
    {
        public string VehicleNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; }
        public int? UserId { get; set; }
        public string? ImageUrl { get; set; }
    }
}