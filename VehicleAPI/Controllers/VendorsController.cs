using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VendorsController : ControllerBase
    {
        private readonly IVendorService _vendorService;

        public VendorsController(IVendorService vendorService)
        {
            _vendorService = vendorService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllVendors()
        {
            var vendors = await _vendorService.GetAllVendorsAsync();
            return Ok(vendors);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetVendorById(int id)
        {
            var vendor = await _vendorService.GetVendorByIdAsync(id);
            if (vendor == null) return NotFound(new { message = $"Vendor with ID {id} not found." });
            return Ok(vendor);
        }

        [HttpPost]
        public async Task<IActionResult> CreateVendor([FromBody] CreateVendorDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var created = await _vendorService.CreateVendorAsync(dto);
                return CreatedAtAction(nameof(GetVendorById), new { id = created.VendorId }, created);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateVendor(int id, [FromBody] UpdateVendorDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _vendorService.UpdateVendorAsync(id, dto);
                if (updated == null) return NotFound(new { message = $"Vendor with ID {id} not found." });
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteVendor(int id)
        {
            try
            {
                var deleted = await _vendorService.DeleteVendorAsync(id);
                if (!deleted) return NotFound(new { message = $"Vendor with ID {id} not found." });
                return Ok(new { message = "Vendor deleted successfully." });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}