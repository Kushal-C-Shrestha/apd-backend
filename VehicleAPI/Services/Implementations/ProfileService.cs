using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;

        public ProfileService(AppDbContext db, IWebHostEnvironment env)
        {
            _db = db;
            _env = env;
        }

        public async Task<CustomerProfileResponseDTO> GetProfileAsync(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("User not found.");

            return MapToDTO(user);
        }

        public async Task<CustomerProfileResponseDTO> UpdateProfileAsync(int userId, UpdateProfileDTO dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("User not found.");

            if (dto.Email.Trim() != user.Email)
            {
                var emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim() && u.UserId != userId);
                if (emailExists)
                    throw new Exception("This email is already used by another account.");
            }

            if (dto.Phone.Trim() != user.Phone)
            {
                var phoneExists = await _db.Users.AnyAsync(u => u.Phone == dto.Phone.Trim() && u.UserId != userId);
                if (phoneExists)
                    throw new Exception("This phone number is already used by another account.");
            }

            user.FullName = dto.FullName.Trim();
            user.Email = dto.Email.Trim();
            user.Phone = dto.Phone.Trim();
            user.Address = dto.Address.Trim();

            await _db.SaveChangesAsync();

            return MapToDTO(user);
        }



        private static CustomerProfileResponseDTO MapToDTO(Models.User user)
        {
            return new CustomerProfileResponseDTO
            {
                UserId = user.UserId,

                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address
            };
        }
    }
}