using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/sale")]
    public class SaleController : ControllerBase
    {
        private readonly ISaleService _saleService;

        public SaleController(ISaleService saleService)
        {
            _saleService = saleService;
        }

        [HttpPost]
        public async Task<ActionResult<ApiResponse<SaleResponseDTO>>> CreateSale([FromBody] CreateSaleDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiResponse<SaleResponseDTO>(false, "Invalid request data", null, errors));
            }

            try
            {
                var sale = await _saleService.CreateSaleAsync(dto);
                return Ok(new ApiResponse<SaleResponseDTO>(true, "Sale created successfully", sale, null));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<SaleResponseDTO>(false, ex.Message, null, null));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<SaleResponseDTO>(false, ex.Message, null, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<SaleResponseDTO>(false, ex.Message, null, null));
            }
        }
    }
}