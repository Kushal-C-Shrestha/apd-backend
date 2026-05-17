using System.Collections.Generic;

namespace VehicleAPI.DTOs.Response
{
    public class CustomerHistoryResponseDTO
    {
        public CustomerResponseDTO Customer { get; set; } = null!;
        public List<VehicleResponseDTO> Vehicles { get; set; } = new List<VehicleResponseDTO>();
        public List<AppointmentResponseDto> Appointments { get; set; } = new List<AppointmentResponseDto>();
        public List<SaleResponseDTO> Purchases { get; set; } = new List<SaleResponseDTO>();
    }
}