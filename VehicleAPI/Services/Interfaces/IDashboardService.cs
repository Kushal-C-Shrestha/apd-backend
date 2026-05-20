using System.Threading.Tasks;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<AdminDashboardDTO> GetAdminDashboardAsync();
        Task<StaffDashboardDTO> GetStaffDashboardAsync();
    }
}