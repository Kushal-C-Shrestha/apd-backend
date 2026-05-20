using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<ActionResult<ApiResponse<AdminDashboardDTO>>> GetAdminDashboard()
        {
            try
            {
                var data = await _dashboardService.GetAdminDashboardAsync();
                return Ok(new ApiResponse<AdminDashboardDTO>(true, "Admin dashboard loaded", data, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<AdminDashboardDTO>(false, ex.Message, null, null));
            }
        }

        [Authorize(Roles = "Staff,Admin")]
        [HttpGet("staff")]
        public async Task<ActionResult<ApiResponse<StaffDashboardDTO>>> GetStaffDashboard()
        {
            try
            {
                var data = await _dashboardService.GetStaffDashboardAsync();
                return Ok(new ApiResponse<StaffDashboardDTO>(true, "Staff dashboard loaded", data, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<StaffDashboardDTO>(false, ex.Message, null, null));
            }
        }
    }
}