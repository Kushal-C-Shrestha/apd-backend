using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/request")]
    public class PartRequestController : ControllerBase
    {
        private readonly IPartRequestService _service;

        public PartRequestController(IPartRequestService service)
        {
            _service = service;
        }

        // POST /api/request
        [HttpPost]
        public async Task<ActionResult<ApiResponse<PartRequestResponseDTO>>> Create([FromBody] CreatePartRequestDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiResponse<PartRequestResponseDTO>(false, "Invalid request data", null, errors));
            }

            try
            {
                var result = await _service.CreateRequestAsync(dto);
                return Ok(new ApiResponse<PartRequestResponseDTO>(true, "Part request created successfully", result, null));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<PartRequestResponseDTO>(false, ex.Message, null, null));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<PartRequestResponseDTO>(false, ex.Message, null, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<PartRequestResponseDTO>(false, ex.Message, null, null));
            }
        }

        // GET /api/request/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<ApiResponse<List<PartRequestResponseDTO>>>> GetByUser(int userId)
        {
            try
            {
                var result = await _service.GetRequestsByUserAsync(userId);
                return Ok(new ApiResponse<List<PartRequestResponseDTO>>(true, "Part requests retrieved successfully", result, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<PartRequestResponseDTO>>(false, ex.Message, null, null));
            }
        }

        // GET /api/request
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<PartRequestResponseDTO>>>> GetAll()
        {
            try
            {
                var result = await _service.GetAllRequestsAsync();
                return Ok(new ApiResponse<List<PartRequestResponseDTO>>(true, "Part requests retrieved successfully", result, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<PartRequestResponseDTO>>(false, ex.Message, null, null));
            }
        }

    }
}
