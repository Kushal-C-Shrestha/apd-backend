using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IAppointmentService
    {
        Task<ApiResponse<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentDto dto);
        Task<ApiResponse<List<AppointmentResponseDto>>> GetAllAppointmentsAsync();
        Task<ApiResponse<List<AppointmentResponseDto>>> GetAppointmentsByUserIdAsync(int userId);
        Task<ApiResponse<AppointmentResponseDto>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentDto dto);
        Task<ApiResponse<AppointmentResponseDto>> CancelAppointmentAsync(int appointmentId);
    }
}
