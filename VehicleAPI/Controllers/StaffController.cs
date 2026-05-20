using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly IStaffService _staffService;

        public StaffController(IStaffService staffService)
        {
            _staffService = staffService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = await _staffService.GetAllStaffAsync();
                return Ok(new { success = true, data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterStaffDTO dto)
        {
            try
            {
                var result = await _staffService.RegisterStaffAsync(dto);
                return Ok(new { success = true, message = "Staff registered successfully.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("{userId}")]
        public async Task<IActionResult> Update(int userId, [FromBody] UpdateStaffDTO dto)
        {
            try
            {
                var result = await _staffService.UpdateStaffAsync(userId, dto);
                return Ok(new { success = true, message = "Staff updated successfully.", data = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpDelete("{userId}")]
        public async Task<IActionResult> Delete(int userId)
        {
            try
            {
                await _staffService.DeleteStaffAsync(userId);
                return Ok(new { success = true, message = "Staff deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }
    }
}