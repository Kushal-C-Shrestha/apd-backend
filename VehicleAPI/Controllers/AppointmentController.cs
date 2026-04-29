using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AppointmentController(IAppointmentService appointmentService) : ControllerBase
    {
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            var result = await appointmentService.CreateAppointmentAsync(dto);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await appointmentService.GetAllAppointmentsAsync();
            return Ok(result);
        }

        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var result = await appointmentService.GetAppointmentsByUserIdAsync(userId);
            return Ok(result);
        }
    }
}
