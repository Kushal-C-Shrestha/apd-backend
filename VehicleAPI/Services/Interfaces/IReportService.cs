using System.Threading.Tasks;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IReportService
    {
        Task<FinancialReportResponseDTO> GetFinancialReportAsync(string timeframe);
        Task<CustomerReportsResponseDTO> GetCustomerReportsAsync();
    }
}