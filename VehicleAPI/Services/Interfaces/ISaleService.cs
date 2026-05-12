using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponseDTO> CreateSaleAsync(CreateSaleDTO dto);
    }
}