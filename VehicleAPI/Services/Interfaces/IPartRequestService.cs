using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IPartRequestService
    {
        Task<PartRequestResponseDTO> CreateRequestAsync(CreatePartRequestDTO dto);
        Task<List<PartRequestResponseDTO>> GetRequestsByUserAsync(int userId);
        Task<List<PartRequestResponseDTO>> GetAllRequestsAsync();
    }
}
