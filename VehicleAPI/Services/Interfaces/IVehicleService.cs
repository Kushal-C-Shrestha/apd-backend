using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IVehicleService
    {
        Task<IEnumerable<VehicleResponseDTO>> GetMyVehiclesAsync(int userId);
        Task<IEnumerable<VehicleResponseDTO>> GetAllVehiclesAsync();
        Task<VehicleResponseDTO> CreateVehicleAsync(int userId, SaveVehicleDTO dto);
        Task<VehicleResponseDTO> UpdateVehicleAsync(int userId, int vehicleId, SaveVehicleDTO dto);
        Task DeleteVehicleAsync(int userId, int vehicleId);
    }
}