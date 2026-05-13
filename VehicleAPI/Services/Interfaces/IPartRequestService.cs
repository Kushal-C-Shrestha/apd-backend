using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IPartRequestService
    {
        Task<PartRequestResponseDTO> CreateRequestAsync(CreatePartRequestDTO dto);
    }
}
