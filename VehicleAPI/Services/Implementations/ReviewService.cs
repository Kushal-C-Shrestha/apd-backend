using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class ReviewService : IReviewService
    {
        private readonly AppDbContext _db;

        public ReviewService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<ReviewResponseDTO> CreateReviewAsync(CreateReviewDTO dto)
        {
            if (dto.Rating < 1 || dto.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            var user = await _db.Users.FindAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            var appointment = await _db.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentId == dto.AppointmentId && a.UserId == dto.UserId);
            if (appointment == null)
                throw new KeyNotFoundException("Appointment not found or does not belong to this user.");

            using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var alreadyReviewed = await _db.Reviews
                    .AnyAsync(r => r.AppointmentId == dto.AppointmentId);
                if (alreadyReviewed)
                    throw new InvalidOperationException("A review for this appointment already exists.");

                var review = new Review
                {
                    UserId = dto.UserId,
                    AppointmentId = dto.AppointmentId,
                    Rating = dto.Rating,
                    Comment = dto.Comment,
                    CreatedAt = DateTime.UtcNow
                };

                _db.Reviews.Add(review);
                await _db.SaveChangesAsync();

                await _db.Entry(review).Reference(r => r.User).LoadAsync();
                await _db.Entry(review).Reference(r => r.Appointment).LoadAsync();

                // Add notification for admin
                var adminUser = await _db.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
                if (adminUser != null)
                {
                    var customerName = review.User?.FullName ?? "A customer";
                    var previewMsg = string.IsNullOrWhiteSpace(review.Comment) 
                        ? $"left a {review.Rating}-star rating." 
                        : $"wrote: \"{(review.Comment.Length > 60 ? review.Comment[..60] + "..." : review.Comment)}\"";

                    _db.Notifications.Add(new Notification
                    {
                        UserId = adminUser.UserId,
                        Message = $"New {review.Rating}-star review from {customerName} — {previewMsg}",
                        CreatedAt = DateTime.UtcNow
                    });
                    await _db.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return MapToDTO(review);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<List<ReviewResponseDTO>> GetAllReviewsAsync()
        {
            var reviews = await _db.Reviews
                .Include(r => r.User)
                .Include(r => r.Appointment)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(MapToDTO).ToList();
        }

        public async Task<List<ReviewResponseDTO>> GetReviewsByUserIdAsync(int userId)
        {
            var reviews = await _db.Reviews
                .Include(r => r.User)
                .Include(r => r.Appointment)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return reviews.Select(MapToDTO).ToList();
        }

        private static ReviewResponseDTO MapToDTO(Review r)
        {
            var appt = r.Appointment;
            string? appointmentLabel = null;

            if (appt != null)
            {
                var dateStr = appt.AppointmentDateTime.ToString("d MMM yyyy");
                appointmentLabel = $"{appt.ServiceType ?? "Service"} — {dateStr}";
            }

            return new ReviewResponseDTO
            {
                ReviewId = r.ReviewId,
                UserId = r.UserId,
                UserName = r.User?.FullName ?? string.Empty,
                AppointmentId = r.AppointmentId,
                AppointmentLabel = appointmentLabel,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            };
        }
    }
}
