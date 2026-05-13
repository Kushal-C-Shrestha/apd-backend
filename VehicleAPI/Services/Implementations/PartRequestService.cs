using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
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
        private readonly IConfiguration _config;

        public PartRequestService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
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

                return MapToDTO(request, part.Name, part.StockQuantity);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<PartRequestResponseDTO>> GetRequestsByUserAsync(int userId)
        {
            var requests = await _db.PartRequests
                .Include(r => r.Part)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(r => MapToDTO(r, r.Part?.Name ?? "Unknown", r.Part?.StockQuantity ?? 0)).ToList();
        }

        public async Task<List<PartRequestResponseDTO>> GetAllRequestsAsync()
        {
            var requests = await _db.PartRequests
                .Include(r => r.Part)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return requests.Select(r => MapToDTO(r, r.Part?.Name ?? "Unknown", r.Part?.StockQuantity ?? 0)).ToList();
        }


        private static PartRequestResponseDTO MapToDTO(PartRequest r, string partName, int stockQuantity) =>
            new()
            {
                RequestId = r.RequestId,
                UserId = r.UserId,
                PartId = r.PartId,
                PartName = partName,
                PartStockQuantity = stockQuantity,
                Quantity = r.Quantity,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            };
    }
}
