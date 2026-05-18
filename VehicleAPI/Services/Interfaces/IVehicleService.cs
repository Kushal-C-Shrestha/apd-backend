using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<VehicleResponseDTO?> GetMyVehicleAsync(int userId);
        Task<VehicleResponseDTO> SaveVehicleAsync(int userId, SaveVehicleDTO dto);
    }
}