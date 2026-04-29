using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<CustomerResponseDTO> RegisterCustomerAsync(RegisterCustomerDTO dto);
        Task<List<CustomerResponseDTO>> SearchCustomersAsync(string query, string filter);
    }
}
