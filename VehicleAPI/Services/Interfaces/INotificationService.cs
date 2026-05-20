using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface INotificationService
    {
        Task<NotificationsResponseDTO> GetAdminNotificationsAsync();
        Task<NotificationsResponseDTO> GetStaffNotificationsAsync();
        Task<NotificationsResponseDTO> GetCustomerNotificationsAsync(int userId);
    }
}