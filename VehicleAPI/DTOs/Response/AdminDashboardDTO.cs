using System.Collections.Generic;

namespace VehicleAPI.DTOs.Response
{
    public class AdminDashboardDTO
    {
        public decimal TotalRevenue { get; set; }
        public int PartsInStock { get; set; }
        public int TotalStaff { get; set; }
        public decimal PendingCredits { get; set; }
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
        public List<SaleResponseDTO> RecentSales { get; set; } = new List<SaleResponseDTO>();
        public List<PartResponseDTO> LowStockParts { get; set; } = new List<PartResponseDTO>();
    }
}