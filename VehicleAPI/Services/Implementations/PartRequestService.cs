using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class PartRequestService : IPartRequestService
    {
        private readonly AppDbContext _db;

        public PartRequestService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PartRequestResponseDTO> CreateRequestAsync(CreatePartRequestDTO dto)
        {
            if (dto.Quantity < 1)
                throw new ArgumentException("Quantity must be at least 1.");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            var part = await _db.Parts.FindAsync(dto.PartId);
            if (part == null)
                throw new KeyNotFoundException($"Part with ID {dto.PartId} not found.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var request = new PartRequest
                {
                    UserId = dto.UserId,
                    PartId = dto.PartId,
                    Quantity = dto.Quantity,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                _db.PartRequests.Add(request);
                await _db.SaveChangesAsync();

                await transaction.CommitAsync();

                return MapToDTO(request, part.Name);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private static PartRequestResponseDTO MapToDTO(PartRequest r, string partName) =>
            new()
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                PartId = r.PartId,
                PartName = partName,
                Quantity = r.Quantity,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            };
    }
}
