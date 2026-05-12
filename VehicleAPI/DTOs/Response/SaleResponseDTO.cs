namespace VehicleAPI.DTOs.Response
{
    public class SaleResponseDTO
    {
        public int SaleId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public string PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<SaleItemResponseDTO> Items { get; set; }
        public CreditResponseDTO? Credit { get; set; }
    }

    public class SaleItemResponseDTO
    {
        public int SaleItemId { get; set; }
        public int PartId { get; set; }
        public string PartName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class CreditResponseDTO
    {
        public int CreditId { get; set; }
        public decimal AmountDue { get; set; }
        public bool IsPaid { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}