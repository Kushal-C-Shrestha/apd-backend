using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class VehicleService : IVehicleService
    {
        private readonly AppDbContext _db;

        public VehicleService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<VehicleResponseDTO>> GetMyVehiclesAsync(int userId)
        {
            var vehicles = await _db.Vehicles.Where(v => v.UserId == userId && !v.IsDeleted).ToListAsync();
            return vehicles.Select(MapToDTO);
        }

        public async Task<IEnumerable<VehicleResponseDTO>> GetAllVehiclesAsync()
        {
            var vehicles = await _db.Vehicles
                .Include(v => v.User)
                .Where(v => !v.IsDeleted)
                .OrderBy(v => v.VehicleId)
                .ToListAsync();
            return vehicles.Select(MapToDTO);
        }

        public async Task<VehicleResponseDTO> CreateVehicleAsync(int userId, SaveVehicleDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VehicleNumber))
                throw new Exception("Vehicle number is required.");

            var duplicate = await _db.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber.Trim() && !v.IsDeleted);
            if (duplicate)
                throw new Exception("This vehicle number is already registered.");

            var vehicle = new Vehicle
            {
                UserId = userId,
                VehicleNumber = dto.VehicleNumber.Trim(),
                Brand = dto.Brand.Trim(),
                Model = dto.Model.Trim(),
                Year = dto.Year ?? 0,
                ImageUrl = dto.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                IsDeleted = false
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();
            return MapToDTO(vehicle);
        }

        public async Task<VehicleResponseDTO> UpdateVehicleAsync(int userId, int vehicleId, SaveVehicleDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VehicleNumber))
                throw new Exception("Vehicle number is required.");

            var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.UserId == userId && !v.IsDeleted);
            if (existing == null)
                throw new Exception("Vehicle not found.");

            if (dto.VehicleNumber.Trim() != existing.VehicleNumber)
            {
                var duplicate = await _db.Vehicles.AnyAsync(v =>
                    v.VehicleNumber == dto.VehicleNumber.Trim() && v.VehicleId != existing.VehicleId && !v.IsDeleted);
                if (duplicate)
                    throw new Exception("This vehicle number is already registered.");
            }

            existing.VehicleNumber = dto.VehicleNumber.Trim();
            existing.Brand = dto.Brand.Trim();
            existing.Model = dto.Model.Trim();
            existing.Year = dto.Year ?? 0;
            existing.ImageUrl = dto.ImageUrl;

            await _db.SaveChangesAsync();
            return MapToDTO(existing);
        }

        public async Task DeleteVehicleAsync(int userId, int vehicleId)
        {
            var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == vehicleId && v.UserId == userId && !v.IsDeleted);
            if (existing == null)
                throw new Exception("Vehicle not found.");

            existing.IsDeleted = true;
            await _db.SaveChangesAsync();
        }

        private static VehicleResponseDTO MapToDTO(Vehicle vehicle)
        {
            return new VehicleResponseDTO
            {
                VehicleId = vehicle.VehicleId,
                VehicleNumber = vehicle.VehicleNumber,
                Brand = vehicle.Brand,
                Model = vehicle.Model,
                Year = vehicle.Year == 0 ? null : vehicle.Year,
                UserId = vehicle.UserId,
                ImageUrl = vehicle.ImageUrl,
                OwnerName = vehicle.User?.FullName
            };
        }
    }
}