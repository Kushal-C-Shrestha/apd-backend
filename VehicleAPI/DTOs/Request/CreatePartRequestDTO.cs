namespace VehicleAPI.DTOs.Request
{
    public class CreatePartRequestDTO
    {
        public int UserId { get; set; }
        public int PartId { get; set; }
        public int Quantity { get; set; }
    }
}
