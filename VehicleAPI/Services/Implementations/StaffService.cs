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
    public class StaffService : IStaffService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public StaffService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<List<StaffResponseDTO>> GetAllStaffAsync()
        {
            var staff = await _db.Users
                .Where(u => u.RoleId == 2)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            return staff.Select(MapToDTO).ToList();
        }

        public async Task<StaffResponseDTO> RegisterStaffAsync(RegisterStaffDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim()))
                throw new Exception("A user with this email already exists.");

            if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone.Trim()))
                throw new Exception("A user with this phone number already exists.");

            string passwordHash = HashPassword(dto.Password);

            var user = new User
            {
                FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}",
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Address = dto.Address?.Trim() ?? string.Empty,
                PasswordHash = passwordHash,
                RoleId = 2,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await SendStaffWelcomeEmailAsync(user.Email, dto.FirstName.Trim(), dto.Email.Trim(), dto.Password);

            return MapToDTO(user);
        }

        public async Task<StaffResponseDTO> UpdateStaffAsync(int userId, UpdateStaffDTO dto)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == 2);
            if (user == null)
                throw new Exception("Staff member not found.");

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

            user.FullName = $"{dto.FirstName.Trim()} {dto.LastName.Trim()}";
            user.Email = dto.Email.Trim();
            user.Phone = dto.Phone.Trim();
            user.Address = dto.Address?.Trim() ?? string.Empty;
            await _db.SaveChangesAsync();
            return MapToDTO(user);
        }

        public async Task DeleteStaffAsync(int userId)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId && u.RoleId == 2);
            if (user == null)
                throw new Exception("Staff member not found.");

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
        }

        private async Task SendStaffWelcomeEmailAsync(string toEmail, string firstName, string email, string password)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"];
                var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

                var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser!, fromName),
                    Subject = "Welcome to Vehicle Service Center – Your Staff Account",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;'>
                            <div style='background:#2563eb;padding:24px;text-align:center;'>
                                <h2 style='color:white;margin:0;'>Vehicle Service Center</h2>
                            </div>
                            <div style='padding:28px;'>
                                <p style='font-size:16px;'>Hello <strong>{firstName}</strong>,</p>
                                <p>You have been registered as a staff member. Here are your login credentials:</p>
                                <div style='background:#f1f5f9;border-radius:6px;padding:16px;margin:16px 0;'>
                                    <p style='margin:4px 0;'><strong>Email:</strong> {email}</p>
                                    <p style='margin:4px 0;'><strong>Password:</strong> {password}</p>
                                </div>
                                <p style='color:#6b7280;font-size:13px;'>Please change your password after your first login.</p>
                            </div>
                        </div>"
                };

                mail.To.Add(toEmail);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Staff email failed: {ex.Message}");
            }
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static StaffResponseDTO MapToDTO(User user)
        {
            var nameParts = user.FullName.Split(' ', 2);
            return new StaffResponseDTO
            {
                UserId = user.UserId,
                FullName = user.FullName,
                FirstName = nameParts.Length > 0 ? nameParts[0] : user.FullName,
                LastName = nameParts.Length > 1 ? nameParts[1] : "",
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address ?? string.Empty,
                CreatedAt = user.CreatedAt
            };
        }
    }
}