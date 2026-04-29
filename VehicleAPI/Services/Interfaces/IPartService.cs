using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IPartService
    {
        Task<List<PartResponseDTO>> GetAllPartsAsync();
        Task<PartResponseDTO?> GetPartByIdAsync(int partId);
        Task<PartResponseDTO> CreatePartAsync(CreatePartDTO dto);
        Task<PartResponseDTO?> UpdatePartAsync(int partId, UpdatePartDTO dto);
        Task<bool> DeletePartAsync(int partId);

        Task<PurchaseResponseDTO> CreatePurchaseAsync(CreatePurchaseDTO dto);
        Task<List<PurchaseResponseDTO>> GetAllPurchasesAsync();
        Task<PurchaseResponseDTO?> GetPurchaseByIdAsync(int purchaseId);
    }
}