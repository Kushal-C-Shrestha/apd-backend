using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [Authorize(Roles = "Admin,Staff")]
    [ApiController]
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("financial")]
        public async Task<ActionResult<ApiResponse<FinancialReportResponseDTO>>> GetFinancialReport([FromQuery] string timeframe = "All Time")
        {
            try
            {
                var report = await _reportService.GetFinancialReportAsync(timeframe);
                return Ok(new ApiResponse<FinancialReportResponseDTO>(true, "Financial report generated successfully", report, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<FinancialReportResponseDTO>(false, ex.Message, null, null));
            }
        }

        [HttpGet("customers")]
        public async Task<ActionResult<ApiResponse<CustomerReportsResponseDTO>>> GetCustomerReports()
        {
            try
            {
                var report = await _reportService.GetCustomerReportsAsync();
                return Ok(new ApiResponse<CustomerReportsResponseDTO>(true, "Customer reports generated successfully", report, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<CustomerReportsResponseDTO>(false, ex.Message, null, null));
            }
        }
    }
}