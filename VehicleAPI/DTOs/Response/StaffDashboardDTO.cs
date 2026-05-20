using System.Collections.Generic;

namespace VehicleAPI.DTOs.Response
{
    public class StaffDashboardDTO
    {
        public decimal TodaySalesSum { get; set; }
        public decimal TotalSalesSum { get; set; }
        public int TotalCustomersCount { get; set; }
        public decimal PendingCreditsSum { get; set; }
        public int TodayAppointmentsCount { get; set; }
        public List<decimal> RevenueChartData { get; set; } = new List<decimal>();
        public Dictionary<string, int> AppointmentStatusCounts { get; set; } = new Dictionary<string, int>();
        public List<SaleResponseDTO> RecentSales { get; set; } = new List<SaleResponseDTO>();
    }
}