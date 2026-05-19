namespace VehicleAPI.DTOs.Response
{
    public class PartRequestResponseDTO
    {
        public int RequestId { get; set; }
        public int UserId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; } = string.Empty;
        public int PartStockQuantity { get; set; }
        public int Quantity { get; set; }
        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
