using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        public async Task<LoginResponseDTO> LoginAsync(LoginDTO dto)
        {
            var identifier = dto.Identifier.Trim().ToLower();

            var user = await _db.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email.ToLower() == identifier ||
                    u.Phone == dto.Identifier.Trim());

            if (user == null)
                throw new Exception("identifier|No account found with this email or phone number.");

            var hashedInput = HashPassword(dto.Password);
            if (user.PasswordHash != hashedInput)
                throw new Exception("password|Incorrect password. Please try again.");

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();
            
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return new LoginResponseDTO
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.Name,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponseDTO> CustomerSelfRegisterAsync(CustomerSelfRegisterDTO dto)
        {
            if (await _db.Users.AnyAsync(u => u.Email == dto.Email.Trim()))
                throw new Exception("email|An account with this email already exists.");

            if (await _db.Users.AnyAsync(u => u.Phone == dto.Phone.Trim()))
                throw new Exception("phone|An account with this phone number already exists.");

            string passwordHash = HashPassword(dto.Password);

            var user = new User
            {
                FullName = dto.FullName.Trim(),
                Email = dto.Email.Trim(),
                Phone = dto.Phone.Trim(),
                Address = string.Empty,
                PasswordHash = passwordHash,
                RoleId = 3,
                CreatedAt = DateTime.UtcNow
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            await _db.Entry(user).Reference(u => u.Role).LoadAsync();

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return new LoginResponseDTO
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone,
                Role = user.Role.Name,
                Token = token,
                RefreshToken = refreshToken
            };
        }

        public async Task ForgotPasswordAsync(ForgotPasswordDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                throw new Exception("No account found with this email address.");

            var oldOtps = _db.OtpRecords.Where(o => o.Email == email && !o.IsUsed);
            _db.OtpRecords.RemoveRange(oldOtps);

            var otp = new Random().Next(100000, 999999).ToString();

            _db.OtpRecords.Add(new OtpRecord
            {
                Email = email,
                OtpCode = otp,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                IsUsed = false
            });

            await _db.SaveChangesAsync();

            await SendOtpEmailAsync(email, user.FullName, otp);
        }

        public async Task VerifyOtpAsync(VerifyOtpDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var record = await _db.OtpRecords
                .Where(o => o.Email == email && o.OtpCode == dto.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (record == null)
                throw new Exception("Invalid OTP. Please check and try again.");

            if (record.ExpiresAt < DateTime.UtcNow)
                throw new Exception("OTP has expired. Please request a new one.");
        }

        public async Task ResetPasswordAsync(ResetPasswordDTO dto)
        {
            var email = dto.Email.Trim().ToLower();

            var record = await _db.OtpRecords
                .Where(o => o.Email == email && o.OtpCode == dto.OtpCode && !o.IsUsed)
                .OrderByDescending(o => o.ExpiresAt)
                .FirstOrDefaultAsync();

            if (record == null)
                throw new Exception("Invalid or expired OTP.");

            if (record.ExpiresAt < DateTime.UtcNow)
                throw new Exception("OTP has expired. Please request a new one.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email);
            if (user == null)
                throw new Exception("User not found.");

            user.PasswordHash = HashPassword(dto.NewPassword);
            record.IsUsed = true;

            await _db.SaveChangesAsync();
        }

        private async Task SendOtpEmailAsync(string toEmail, string fullName, string otp)
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
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser!, fromName),
                    Subject = "Password Reset OTP – Vehicle Service Center",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;'>
                            <div style='background:#2563eb;padding:24px;text-align:center;'>
                                <h2 style='color:white;margin:0;'>Vehicle Service Center</h2>
                            </div>
                            <div style='padding:28px;'>
                                <p style='font-size:16px;'>Hello <strong>{fullName}</strong>,</p>
                                <p>You requested a password reset. Use the OTP below. It expires in <strong>10 minutes</strong>.</p>
                                <div style='background:#f1f5f9;border-radius:8px;padding:20px;margin:20px 0;text-align:center;'>
                                    <p style='font-size:36px;font-weight:700;letter-spacing:10px;color:#2563eb;margin:0;'>{otp}</p>
                                </div>
                                <p style='color:#6b7280;font-size:13px;'>If you did not request this, please ignore this email.</p>
                            </div>
                        </div>"
                };

                mail.To.Add(toEmail);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OTP email sending failed: {ex.Message}");
            }
        }



        private string GenerateJwtToken(User user)
        {
            var jwtKey = _config["Jwt:Key"]!;
            var jwtIssuer = _config["Jwt:Issuer"]!;
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim("CustomerId", user.UserId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtIssuer,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(15),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private static string GenerateRefreshToken()
        {
            return Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(64));
        }

        public async Task UpdateUserRefreshTokenAsync(int userId, string refreshToken, DateTime expiryTime)
        {
            var user = await _db.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = refreshToken;
                user.RefreshTokenExpiryTime = expiryTime;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<(string AccessToken, string RefreshToken)> RefreshTokenAsync(string refreshToken)
        {
            var user = await _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new Exception("Invalid or expired refresh token.");

            var newAccessToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _db.SaveChangesAsync();

            return (newAccessToken, newRefreshToken);
        }
    }
}
