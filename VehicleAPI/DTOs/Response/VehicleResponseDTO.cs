namespace VehicleAPI.DTOs.Response
{
    public class VehicleResponseDTO
    {
        public int VehicleId { get; set; }
        public string VehicleNumber { get; set; } = string.Empty;
        public string Brand { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public int? Year { get; set; }
        public int UserId { get; set; }
        public string? ImageUrl { get; set; }
        public string? OwnerName { get; set; }
    }
}