using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IVendorService
    {
        Task<List<VendorResponseDTO>> GetAllVendorsAsync();
        Task<VendorResponseDTO?> GetVendorByIdAsync(int vendorId);
        Task<VendorResponseDTO> CreateVendorAsync(CreateVendorDTO dto);
        Task<VendorResponseDTO?> UpdateVendorAsync(int vendorId, UpdateVendorDTO dto);
        Task<bool> DeleteVendorAsync(int vendorId);
    }
}