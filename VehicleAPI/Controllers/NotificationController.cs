using Microsoft.AspNetCore.Mvc;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /api/notifications/admin
        [HttpGet("admin")]
        public async Task<IActionResult> GetAdminNotifications()
        {
            try
            {
                var result = await _notificationService.GetAdminNotificationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/notifications/staff
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaffNotifications()
        {
            try
            {
                var result = await _notificationService.GetStaffNotificationsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/notifications/customer/{userId}
        [HttpGet("customer/{userId:int}")]
        public async Task<IActionResult> GetCustomerNotifications(int userId)
        {
            try
            {
                var result = await _notificationService.GetCustomerNotificationsAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}