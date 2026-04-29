using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class PartsController : ControllerBase
	{
		private readonly IPartService _partService;

		public PartsController(IPartService partService)
		{
			_partService = partService;
		}

		[HttpGet]
		public async Task<IActionResult> GetAllParts()
		{
			var parts = await _partService.GetAllPartsAsync();
			return Ok(parts);
		}

		[HttpGet("{id:int}")]
		public async Task<IActionResult> GetPartById(int id)
		{
			var part = await _partService.GetPartByIdAsync(id);
			if (part == null) return NotFound(new { message = $"Part with ID {id} not found." });
			return Ok(part);
		}

		[HttpPost]
		public async Task<IActionResult> CreatePart([FromBody] CreatePartDTO dto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var created = await _partService.CreatePartAsync(dto);
			return CreatedAtAction(nameof(GetPartById), new { id = created.PartId }, created);
		}

		[HttpPut("{id:int}")]
		public async Task<IActionResult> UpdatePart(int id, [FromBody] UpdatePartDTO dto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			var updated = await _partService.UpdatePartAsync(id, dto);
			if (updated == null) return NotFound(new { message = $"Part with ID {id} not found." });
			return Ok(updated);
		}

		[HttpDelete("{id:int}")]
		public async Task<IActionResult> DeletePart(int id)
		{
			try
			{
				var deleted = await _partService.DeletePartAsync(id);
				if (!deleted) return NotFound(new { message = $"Part with ID {id} not found." });
				return Ok(new { message = "Part deleted successfully." });
			}
			catch (InvalidOperationException ex)
			{
				return Conflict(new { message = ex.Message });
			}
		}

		[HttpGet("purchases")]
		public async Task<IActionResult> GetAllPurchases()
		{
			var purchases = await _partService.GetAllPurchasesAsync();
			return Ok(purchases);
		}

		[HttpGet("purchases/{id:int}")]
		public async Task<IActionResult> GetPurchaseById(int id)
		{
			var purchase = await _partService.GetPurchaseByIdAsync(id);
			if (purchase == null) return NotFound(new { message = $"Purchase with ID {id} not found." });
			return Ok(purchase);
		}

		[HttpPost("purchases")]
		public async Task<IActionResult> CreatePurchase([FromBody] CreatePurchaseDTO dto)
		{
			if (!ModelState.IsValid) return BadRequest(ModelState);

			try
			{
				var purchase = await _partService.CreatePurchaseAsync(dto);
				return CreatedAtAction(nameof(GetPurchaseById), new { id = purchase.PurchaseId }, purchase);
			}
			catch (KeyNotFoundException ex)
			{
				return NotFound(new { message = ex.Message });
			}
		}
	}
}