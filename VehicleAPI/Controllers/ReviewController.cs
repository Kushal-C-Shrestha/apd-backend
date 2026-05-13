using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/review")]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _service;

        public ReviewController(IReviewService service)
        {
            _service = service;
        }

        // POST /api/review
        [HttpPost]
        public async Task<ActionResult<ApiResponse<ReviewResponseDTO>>> CreateReview([FromBody] CreateReviewDTO dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(new ApiResponse<ReviewResponseDTO>(false, "Invalid request data", null, errors));
            }

            try
            {
                var result = await _service.CreateReviewAsync(dto);
                return Ok(new ApiResponse<ReviewResponseDTO>(true, "Review created successfully", result, null));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new ApiResponse<ReviewResponseDTO>(false, ex.Message, null, null));
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<ReviewResponseDTO>(false, ex.Message, null, null));
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new ApiResponse<ReviewResponseDTO>(false, ex.Message, null, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<ReviewResponseDTO>(false, ex.Message, null, null));
            }
        }
        // GET /api/review
        [HttpGet]
        public async Task<ActionResult<ApiResponse<List<ReviewResponseDTO>>>> GetAllReviews()
        {
            try
            {
                var result = await _service.GetAllReviewsAsync();
                return Ok(new ApiResponse<List<ReviewResponseDTO>>(true, "Reviews retrieved successfully", result, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ReviewResponseDTO>>(false, ex.Message, null, null));
            }
        }

        // GET /api/review/user/{userId}
        [HttpGet("user/{userId:int}")]
        public async Task<ActionResult<ApiResponse<List<ReviewResponseDTO>>>> GetReviewsByUserId(int userId)
        {
            try
            {
                var result = await _service.GetReviewsByUserIdAsync(userId);
                return Ok(new ApiResponse<List<ReviewResponseDTO>>(true, "Reviews retrieved successfully", result, null));
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<ReviewResponseDTO>>(false, ex.Message, null, null));
            }
        }
    }
}
