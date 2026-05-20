using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface ISaleService
    {
        Task<SaleResponseDTO> CreateSaleAsync(CreateSaleDTO dto);
        Task<List<SaleResponseDTO>> GetAllSalesAsync();
        Task<SaleResponseDTO?> GetSaleByIdAsync(int saleId);
        Task<List<SaleResponseDTO>> GetSalesByUserIdAsync(int userId);
        Task<SaleResponseDTO?> SettleCreditAsync(int saleId);
        Task<bool> SendInvoiceEmailAsync(int saleId);
    }
}