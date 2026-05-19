using System.Collections.Generic;

namespace VehicleAPI.DTOs.Response
{
    public class CustomerReportsResponseDTO
    {
        public List<RegularCustomerDTO> Regulars { get; set; } = new();
        public List<HighSpenderDTO> HighSpenders { get; set; } = new();
        public List<PendingCreditCustomerDTO> PendingCredits { get; set; } = new();
    }

    public class RegularCustomerDTO
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int VisitCount { get; set; }
    }

    public class HighSpenderDTO
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal TotalSpent { get; set; }
    }

    public class PendingCreditCustomerDTO
    {
        public int UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal PendingCredit { get; set; }
    }
}