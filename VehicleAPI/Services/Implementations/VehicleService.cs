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

        public async Task<VehicleResponseDTO?> GetMyVehicleAsync(int userId)
        {
            var vehicle = await _db.Vehicles.FirstOrDefaultAsync(v => v.UserId == userId);
            if (vehicle == null) return null;
            return MapToDTO(vehicle);
        }

        public async Task<VehicleResponseDTO> SaveVehicleAsync(int userId, SaveVehicleDTO dto)
        {
            if (string.IsNullOrWhiteSpace(dto.VehicleNumber))
                throw new Exception("Vehicle number is required.");

            var existing = await _db.Vehicles.FirstOrDefaultAsync(v => v.UserId == userId);

            if (existing != null)
            {
                if (dto.VehicleNumber.Trim() != existing.VehicleNumber)
                {
                    var duplicate = await _db.Vehicles.AnyAsync(v =>
                        v.VehicleNumber == dto.VehicleNumber.Trim() && v.VehicleId != existing.VehicleId);
                    if (duplicate)
                        throw new Exception("This vehicle number is already registered.");
                }

                existing.VehicleNumber = dto.VehicleNumber.Trim();
                existing.Brand = dto.Brand.Trim();
                existing.Model = dto.Model.Trim();
                existing.Year = dto.Year ?? 0;

                await _db.SaveChangesAsync();
                return MapToDTO(existing);
            }
            else
            {
                var duplicate = await _db.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber.Trim());
                if (duplicate)
                    throw new Exception("This vehicle number is already registered.");

                var vehicle = new Vehicle
                {
                    UserId = userId,
                    VehicleNumber = dto.VehicleNumber.Trim(),
                    Brand = dto.Brand.Trim(),
                    Model = dto.Model.Trim(),
                    Year = dto.Year ?? 0,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Vehicles.Add(vehicle);
                await _db.SaveChangesAsync();
                return MapToDTO(vehicle);
            }
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
                UserId = vehicle.UserId
            };
        }
    }
}