using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IProfileService
    {
        Task<CustomerProfileResponseDTO> GetProfileAsync(int userId);
        Task<CustomerProfileResponseDTO> UpdateProfileAsync(int userId, UpdateProfileDTO dto);
    }
}