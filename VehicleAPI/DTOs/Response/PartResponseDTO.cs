namespace VehicleAPI.DTOs.Response
{
    public class PartResponseDTO
    {
        public int PartId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PurchaseItemResponseDTO
    {
        public int PurchaseItemId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class PurchaseResponseDTO
    {
        public int PurchaseId { get; set; }
        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<PurchaseItemResponseDTO> Items { get; set; }
    }
}