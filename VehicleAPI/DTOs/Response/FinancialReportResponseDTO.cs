using System.Collections.Generic;

namespace VehicleAPI.DTOs.Response
{
    public class FinancialReportResponseDTO
    {
        public decimal TotalRevenue { get; set; }
        public decimal TotalDiscounts { get; set; }
        public decimal PendingCredits { get; set; }
        public int TotalInvoices { get; set; }
        public int PaidSalesCount { get; set; }
        public int UnpaidSalesCount { get; set; }
        public List<LowStockPartDTO> LowStockParts { get; set; } = new();
        public List<TopStockedPartDTO> TopStockedParts { get; set; } = new();
    }

    public class LowStockPartDTO
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class TopStockedPartDTO
    {
        public int PartId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }
}