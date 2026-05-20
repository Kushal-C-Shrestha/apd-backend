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
            if (part == null || part.IsDeleted)
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

                request.User = user;
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
                .Include(r => r.User)
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


        public async Task<PartRequestResponseDTO?> UpdateRequestStatusAsync(int requestId, string status)
        {
            var allowed = new[] { "Pending", "Approved", "Rejected", "Fulfilled", "Available" };
            if (!allowed.Contains(status))
                throw new ArgumentException("Invalid status.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var request = await _db.PartRequests
                    .Include(r => r.Part)
                    .Include(r => r.User)
                    .FirstOrDefaultAsync(r => r.RequestId == requestId);

                if (request == null) return null;

                if ((status == "Approved" || status == "Fulfilled" || status == "Available") && request.Part != null)
                {
                    if (request.Part.StockQuantity < request.Quantity)
                    {
                        throw new InvalidOperationException($"Cannot settle part request: Insufficient stock. Requested: {request.Quantity}, Available: {request.Part.StockQuantity}. Please restock the part first.");
                    }
                }

                request.Status = status;

                string? message = null;
                if (status == "Available")
                {
                    message = $"Your request for '{request.Part?.Name}' is now available. Please order it soon!";
                }
                else if (status == "Approved")
                {
                    message = $"Your request for '{request.Part?.Name}' has been approved and is available. Please order it soon!";
                }
                else if (status == "Fulfilled")
                {
                    message = $"Your request for '{request.Part?.Name}' has been fulfilled.";
                }
                else if (status == "Rejected")
                {
                    message = $"Your request for '{request.Part?.Name}' has been rejected.";
                }

                if (message != null && request.User != null)
                {
                    _db.Notifications.Add(new Notification
                    {
                        UserId = request.UserId,
                        Message = message,
                        CreatedAt = DateTime.UtcNow
                    });

                    if (!string.IsNullOrEmpty(request.User.Email))
                    {
                        _ = SendPartRequestStatusEmailAsync(request.User.Email, request.User.FullName, request.Part?.Name ?? "Unknown", status);
                    }
                }

                await _db.SaveChangesAsync();
                await transaction.CommitAsync();

                return MapToDTO(request, request.Part?.Name ?? "Unknown", request.Part?.StockQuantity ?? 0);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task SendPartRequestStatusEmailAsync(string toEmail, string toName, string partName, string status)
        {
            try
            {
                var host = _config["Email:SmtpHost"];
                var portStr = _config["Email:SmtpPort"];
                var user = _config["Email:SmtpUser"];
                var pass = _config["Email:SmtpPass"];
                var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
                {
                    return;
                }

                int port = int.Parse(portStr);

                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(user, pass),
                    EnableSsl = true
                };

                string subject = "";
                string body = "";

                if (status == "Available" || status == "Approved")
                {
                    subject = status == "Available" ? "Part Available - Order Now!" : "Part Request Approved!";
                    body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2>Good News, {toName}!</h2>
                            <p>The part you requested, <strong>{partName}</strong>, is now {(status == "Available" ? "available" : "approved and available")} in our stock.</p>
                            <p>Please visit the portal to complete your order soon before it runs out!</p>
                            <br>
                            <p>Best regards,</p>
                            <p>{fromName}</p>
                        </div>";
                }
                else if (status == "Fulfilled")
                {
                    subject = "Part Request Fulfilled!";
                    body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2>Hello, {toName}!</h2>
                            <p>The part you requested, <strong>{partName}</strong>, has been marked as fulfilled.</p>
                            <br>
                            <p>Best regards,</p>
                            <p>{fromName}</p>
                        </div>";
                }
                else if (status == "Rejected")
                {
                    subject = "Part Request Rejected";
                    body = $@"
                        <div style='font-family: Arial, sans-serif; padding: 20px;'>
                            <h2>Hello, {toName}!</h2>
                            <p>We regret to inform you that your request for the part, <strong>{partName}</strong>, has been rejected.</p>
                            <p>If you have any questions or require further assistance, please contact our support team.</p>
                            <br>
                            <p>Best regards,</p>
                            <p>{fromName}</p>
                        </div>";
                }
                else
                {
                    return;
                }

                var mail = new MailMessage
                {
                    From = new MailAddress(user, fromName),
                    Subject = subject,
                    IsBodyHtml = true,
                    Body = body
                };

                mail.To.Add(toEmail);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Part request status {status} email sending failed: {ex.Message}");
            }
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
                CreatedAt = r.CreatedAt,
                CustomerName = r.User?.FullName ?? "Unknown Customer",
                CustomerEmail = r.User?.Email ?? ""
            };
    }
}
