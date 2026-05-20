using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class VehicleController : ControllerBase
    {
        private readonly IVehicleService _vehicleService;

        public VehicleController(IVehicleService vehicleService)
        {
            _vehicleService = vehicleService;
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllVehicles()
        {
            try
            {
                var result = await _vehicleService.GetAllVehiclesAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyVehicles()
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _vehicleService.GetMyVehiclesAsync(userId);
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateVehicle([FromBody] SaveVehicleDTO dto)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var isStaffOrAdmin = User.IsInRole("Staff") || User.IsInRole("Admin");
                var targetUserId = (isStaffOrAdmin && dto.UserId.HasValue) ? dto.UserId.Value : currentUserId;

                var result = await _vehicleService.CreateVehicleAsync(targetUserId, dto);
                return Ok(new { success = true, message = "Vehicle added successfully.", data = result });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already registered"))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = ex.Message,
                        errors = new Dictionary<string, string[]> {
                            { "vehicleNumber", new[] { ex.Message } }
                        }
                    });
                }
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{vehicleId}")]
        public async Task<IActionResult> UpdateVehicle(int vehicleId, [FromBody] SaveVehicleDTO dto)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _vehicleService.UpdateVehicleAsync(userId, vehicleId, dto);
                return Ok(new { success = true, message = "Vehicle updated successfully.", data = result });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("already registered"))
                {
                    return BadRequest(new { 
                        success = false, 
                        message = ex.Message,
                        errors = new Dictionary<string, string[]> {
                            { "vehicleNumber", new[] { ex.Message } }
                        }
                    });
                }
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{vehicleId}")]
        public async Task<IActionResult> DeleteVehicle(int vehicleId)
        {
            try
            {
                var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                await _vehicleService.DeleteVehicleAsync(userId, vehicleId);
                return Ok(new { success = true, message = "Vehicle deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}