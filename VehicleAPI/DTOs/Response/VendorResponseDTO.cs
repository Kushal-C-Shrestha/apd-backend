namespace VehicleAPI.DTOs.Response
{
    public class VendorResponseDTO
    {
        public int VendorId { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public DateTime CreatedAt { get; set; }
        public int TotalPurchases { get; set; }
    }
}