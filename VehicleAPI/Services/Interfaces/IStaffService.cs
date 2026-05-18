using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IStaffService
    {
        Task<List<StaffResponseDTO>> GetAllStaffAsync();
        Task<StaffResponseDTO> RegisterStaffAsync(RegisterStaffDTO dto);
        Task<StaffResponseDTO> UpdateStaffAsync(int userId, UpdateStaffDTO dto);
        Task DeleteStaffAsync(int userId);
    }
}