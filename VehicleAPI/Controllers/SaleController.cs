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
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<SaleResponseDTO>>>> GetAllSales()
        {
            try
            {
                var sales = await _saleService.GetAllSalesAsync();
                return Ok(new ApiResponse<List<SaleResponseDTO>>(true, "Sales retrieved successfully", sales, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<SaleResponseDTO>>(false, ex.Message, null, null));
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<SaleResponseDTO>>> GetSaleById(int id)
        {
            try
            {
                var sale = await _saleService.GetSaleByIdAsync(id);
                if (sale == null) 
                    return NotFound(new ApiResponse<SaleResponseDTO>(false, $"Sale with ID {id} not found.", null, null));
                
                return Ok(new ApiResponse<SaleResponseDTO>(true, "Sale retrieved successfully", sale, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<SaleResponseDTO>(false, ex.Message, null, null));
            }
        }

        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<List<SaleResponseDTO>>>> GetSalesByUser(int userId)
        {
            try
            {
                var sales = await _saleService.GetSalesByUserIdAsync(userId);
                return Ok(new ApiResponse<List<SaleResponseDTO>>(true, "Sales retrieved successfully", sales, null));
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse<List<SaleResponseDTO>>(false, ex.Message, null, null));
            }
        }

        [HttpPut("{id:int}/settle")]
        public async Task<ActionResult<ApiResponse<SaleResponseDTO>>> SettleCredit(int id)
        {
            try
            {
                var sale = await _saleService.SettleCreditAsync(id);
                if (sale == null) 
                    return NotFound(new ApiResponse<SaleResponseDTO>(false, $"Sale with ID {id} not found.", null, null));
                
                return Ok(new ApiResponse<SaleResponseDTO>(true, "Credit settled successfully", sale, null));
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