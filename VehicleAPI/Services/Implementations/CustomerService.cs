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
    public class CustomerService : ICustomerService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public CustomerService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<CustomerResponseDTO> RegisterCustomerAsync(RegisterCustomerDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
                throw new Exception("A customer with this email already exists.");

            if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone))
                throw new Exception("A customer with this phone number already exists.");

            if (await _db.Vehicles.AnyAsync(v => v.VehicleNumber == dto.VehicleNumber))
                throw new Exception("This vehicle number is already registered.");

            string customerId = await GenerateUniqueCustomerIdAsync();
            string rawPassword = GeneratePassword();
            string passwordHash = HashPassword(rawPassword);

            var user = new User
            {
                CustomerId = customerId,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address ?? string.Empty,
                PasswordHash = passwordHash,
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var vehicle = new Vehicle
            {
                VehicleNumber = dto.VehicleNumber,
                Brand = dto.Brand,
                Model = dto.Model,
                Year = dto.Year ?? 0,
                UserId = user.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Vehicles.Add(vehicle);
            await _db.SaveChangesAsync();

            await SendWelcomeEmailAsync(user.Email, user.FullName, customerId, rawPassword);

            return MapToDTO(user, vehicle);
        }

        public async Task<List<CustomerResponseDTO>> SearchCustomersAsync(string query, string filter)
        {
            var usersQuery = _db.Users
                .Include(u => u.Vehicles)
                .Where(u => u.RoleId == 3);

            if (!string.IsNullOrWhiteSpace(query))
            {
                string q = query.Trim().ToLower();

                usersQuery = filter switch
                {
                    "name" => usersQuery.Where(u =>
                        u.FullName.ToLower().Contains(q)),

                    "phone" => usersQuery.Where(u =>
                        u.Phone.ToLower().Contains(q)),

                    "id" => usersQuery.Where(u =>
                        u.CustomerId.ToLower().Contains(q)),

                    "vehicle" => usersQuery.Where(u =>
                        u.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(q))),

                    _ => usersQuery.Where(u =>
                        u.FullName.ToLower().Contains(q) ||
                        u.Phone.ToLower().Contains(q) ||
                        u.CustomerId.ToLower().Contains(q) ||
                        u.Vehicles.Any(v => v.VehicleNumber.ToLower().Contains(q)))
                };
            }

            var users = await usersQuery.ToListAsync();

            return users.Select(u =>
            {
                var vehicle = u.Vehicles.FirstOrDefault();
                return MapToDTO(u, vehicle);
            }).ToList();
        }

        private async Task<string> GenerateUniqueCustomerIdAsync()
        {
            string id;
            var rng = new Random();
            do
            {
                id = "CUS-" + rng.Next(1000, 9999);
            }
            while (await _db.Users.AnyAsync(u => u.CustomerId == id));
            return id;
        }

        private static string GeneratePassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789@#!";
            var rng = new Random();
            return new string(Enumerable.Range(0, 10)
                .Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private async Task SendWelcomeEmailAsync(string toEmail, string fullName, string customerId, string rawPassword)
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
                    Subject = "Welcome to Vehicle Service Center – Your Account Details",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;'>
                            <div style='background:#2563eb;padding:24px;text-align:center;'>
                                <h2 style='color:white;margin:0;'>Vehicle Service Center</h2>
                            </div>
                            <div style='padding:28px;'>
                                <p style='font-size:16px;'>Hello <strong>{fullName}</strong>,</p>
                                <p>Your account has been successfully created. Here are your login credentials:</p>
                                <div style='background:#f1f5f9;border-radius:6px;padding:16px;margin:16px 0;'>
                                    <p style='margin:4px 0;'><strong>Customer ID:</strong> {customerId}</p>
                                    <p style='margin:4px 0;'><strong>Password:</strong> {rawPassword}</p>
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
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }
        }

        private static CustomerResponseDTO MapToDTO(User user, Vehicle? vehicle)
        {
            return new CustomerResponseDTO
            {
                UserId = user.UserId,
                CustomerId = user.CustomerId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                IsActive = true,
                CreatedAt = user.CreatedAt,
                VehicleNumber = vehicle?.VehicleNumber ?? "",
                Brand = vehicle?.Brand ?? "",
                Model = vehicle?.Model ?? "",
                Year = vehicle?.Year
            };
        }
    }
}