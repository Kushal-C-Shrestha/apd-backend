using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDTO> CreateReviewAsync(CreateReviewDTO dto);
        Task<List<ReviewResponseDTO>> GetAllReviewsAsync();
        Task<List<ReviewResponseDTO>> GetReviewsByUserIdAsync(int userId);
    }
}
