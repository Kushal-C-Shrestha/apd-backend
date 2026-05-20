using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VehicleAPI.Data;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class AppointmentService(AppDbContext dbContext, IHttpContextAccessor httpContextAccessor) : IAppointmentService
    {
        private int? GetCurrentUserId()
        {
            var claim = httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
        private static AppointmentResponseDto ToDto(Appointment a) => new()
        {
            AppointmentId = a.AppointmentId,
            UserId = a.UserId,
            UserName = a.User?.FullName ?? string.Empty,
            VehicleId = a.VehicleId,
            VehicleNumber = a.Vehicle?.VehicleNumber ?? string.Empty,
            AppointmentDateTime = a.AppointmentDateTime,
            ServiceType = a.ServiceType,
            Status = a.Status,
            CreatedAt = a.CreatedAt
        };

        public async Task<ApiResponse<AppointmentResponseDto>> CreateAppointmentAsync(CreateAppointmentDto dto)
        {
            var userExists = await dbContext.Users.AnyAsync(u => u.UserId == dto.UserId);
            if (!userExists)
                return new ApiResponse<AppointmentResponseDto>(false, "User not found.", null, null);

            var vehicleExists = await dbContext.Vehicles.AnyAsync(v => v.VehicleId == dto.VehicleId);
            if (!vehicleExists)
                return new ApiResponse<AppointmentResponseDto>(false, "Vehicle not found.", null, null);

            var startTime = new TimeOnly(9, 0);
            var endTime = new TimeOnly(17, 0);
            if (dto.AppointmentTime < startTime || dto.AppointmentTime > endTime)
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment time must be between 9:00 AM and 5:00 PM.", null, null);

            var appointmentDate = dto.AppointmentDate.ToDateTime(TimeOnly.MinValue).Date;
            var hasExisting = await dbContext.Appointments.AnyAsync(a =>
                a.UserId == dto.UserId &&
                a.VehicleId == dto.VehicleId &&
                (a.Status == "Pending" || a.Status == "Confirmed") &&
                a.AppointmentDateTime.Date == appointmentDate);

            if (hasExisting)
                return new ApiResponse<AppointmentResponseDto>(false, "You already have an active appointment booked for this vehicle on this day.", null, null);

            var appointment = new Appointment
            {
                UserId = dto.UserId,
                VehicleId = dto.VehicleId,
                AppointmentDateTime = dto.AppointmentDate.ToDateTime(dto.AppointmentTime),
                ServiceType = string.IsNullOrWhiteSpace(dto.ServiceType) ? "General Service" : dto.ServiceType,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Appointments.AddAsync(appointment);
            await dbContext.SaveChangesAsync();

            // Notify customer
            dbContext.Notifications.Add(new Notification
            {
                UserId = dto.UserId,
                Message = $"Your appointment for {appointment.ServiceType} has been scheduled for {appointment.AppointmentDateTime:g}.",
                CreatedAt = DateTime.UtcNow
            });

            var callerId = GetCurrentUserId();
            if (callerId.HasValue && callerId.Value != dto.UserId)
            {
                var customerName = (await dbContext.Users.FindAsync(dto.UserId))?.FullName ?? "A customer";
                dbContext.Notifications.Add(new Notification
                {
                    UserId = callerId.Value,
                    Message = $"New appointment booking: {customerName} for {appointment.ServiceType} on {appointment.AppointmentDateTime:g}.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync();

            await dbContext.Entry(appointment).Reference(a => a.User).LoadAsync();
            await dbContext.Entry(appointment).Reference(a => a.Vehicle).LoadAsync();

            return new ApiResponse<AppointmentResponseDto>(true, "Appointment created successfully.", ToDto(appointment), null);
        }

        public async Task<ApiResponse<List<AppointmentResponseDto>>> GetAllAppointmentsAsync()
        {
            var appointments = await dbContext.Appointments
                .Include(a => a.User)
                .Include(a => a.Vehicle)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return new ApiResponse<List<AppointmentResponseDto>>(true, "Appointments retrieved successfully.", appointments.Select(ToDto).ToList(), null);
        }

        public async Task<ApiResponse<List<AppointmentResponseDto>>> GetAppointmentsByUserIdAsync(int userId)
        {
            var appointments = await dbContext.Appointments
                .Include(a => a.User)
                .Include(a => a.Vehicle)
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.AppointmentDateTime)
                .ToListAsync();

            return new ApiResponse<List<AppointmentResponseDto>>(true, "Appointments retrieved successfully.", appointments.Select(ToDto).ToList(), null);
        }

        public async Task<ApiResponse<AppointmentResponseDto>> RescheduleAppointmentAsync(int appointmentId, RescheduleAppointmentDto dto)
        {
            var appointment = await dbContext.Appointments
                .Include(a => a.User)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment not found.", null, null);

            if (appointment.AppointmentDateTime <= DateTime.UtcNow)
                return new ApiResponse<AppointmentResponseDto>(false, "Cannot reschedule past appointments.", null, null);

            if (appointment.Status == "Cancelled")
                return new ApiResponse<AppointmentResponseDto>(false, "Cannot reschedule a cancelled appointment.", null, null);

            var startTime = new TimeOnly(9, 0);
            var endTime = new TimeOnly(17, 0);
            if (dto.AppointmentTime < startTime || dto.AppointmentTime > endTime)
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment time must be between 9:00 AM and 5:00 PM.", null, null);

            appointment.AppointmentDateTime = dto.AppointmentDate.ToDateTime(dto.AppointmentTime);
            if (!string.IsNullOrWhiteSpace(dto.ServiceType))
            {
                appointment.ServiceType = dto.ServiceType;
            }

            if (dto.VehicleId.HasValue && dto.VehicleId.Value != appointment.VehicleId)
            {
                var vehicle = await dbContext.Vehicles.FirstOrDefaultAsync(v => v.VehicleId == dto.VehicleId.Value && !v.IsDeleted);
                if (vehicle == null)
                    return new ApiResponse<AppointmentResponseDto>(false, "Selected vehicle not found or has been deleted.", null, null);

                appointment.VehicleId = dto.VehicleId.Value;
                appointment.Vehicle = vehicle;
            }

            await dbContext.SaveChangesAsync();

            // Notify customer
            dbContext.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"Your appointment for {appointment.ServiceType} has been rescheduled to {appointment.AppointmentDateTime:g}.",
                CreatedAt = DateTime.UtcNow
            });

            var callerId = GetCurrentUserId();
            if (callerId.HasValue && callerId.Value != appointment.UserId)
            {
                var customerName = appointment.User?.FullName ?? "A customer";
                dbContext.Notifications.Add(new Notification
                {
                    UserId = callerId.Value,
                    Message = $"Appointment rescheduled: {customerName}'s appointment is now scheduled for {appointment.AppointmentDateTime:g}.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync();

            return new ApiResponse<AppointmentResponseDto>(true, "Appointment rescheduled successfully.", ToDto(appointment), null);
        }

        public async Task<ApiResponse<AppointmentResponseDto>> CancelAppointmentAsync(int appointmentId)
        {
            var appointment = await dbContext.Appointments
                .Include(a => a.User)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment not found.", null, null);

            if (appointment.AppointmentDateTime <= DateTime.UtcNow)
                return new ApiResponse<AppointmentResponseDto>(false, "Cannot cancel past appointments.", null, null);

            if (appointment.Status == "Cancelled")
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment is already cancelled.", null, null);

            appointment.Status = "Cancelled";
            await dbContext.SaveChangesAsync();

            // Notify customer
            dbContext.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"Your appointment for {appointment.ServiceType} on {appointment.AppointmentDateTime:g} has been cancelled.",
                CreatedAt = DateTime.UtcNow
            });

            var callerId = GetCurrentUserId();
            if (callerId.HasValue && callerId.Value != appointment.UserId)
            {
                var customerName = appointment.User?.FullName ?? "A customer";
                dbContext.Notifications.Add(new Notification
                {
                    UserId = callerId.Value,
                    Message = $"Appointment cancelled: {customerName}'s appointment for {appointment.ServiceType} on {appointment.AppointmentDateTime:g} was cancelled.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync();

            return new ApiResponse<AppointmentResponseDto>(true, "Appointment cancelled successfully.", ToDto(appointment), null);
        }

        public async Task<ApiResponse<AppointmentResponseDto>> CompleteAppointmentAsync(int appointmentId)
        {
            var appointment = await dbContext.Appointments
                .Include(a => a.User)
                .Include(a => a.Vehicle)
                .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId);

            if (appointment == null)
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment not found.", null, null);

            if (appointment.Status == "Completed")
                return new ApiResponse<AppointmentResponseDto>(false, "Appointment is already completed.", null, null);

            if (appointment.Status == "Cancelled")
                return new ApiResponse<AppointmentResponseDto>(false, "Cannot complete a cancelled appointment.", null, null);

            if (appointment.AppointmentDateTime.Date > DateTime.UtcNow.Date)
                return new ApiResponse<AppointmentResponseDto>(false, "Cannot mark appointment as completed if the appointment date is in the future.", null, null);

            appointment.Status = "Completed";
            await dbContext.SaveChangesAsync();

            // Notify customer
            dbContext.Notifications.Add(new Notification
            {
                UserId = appointment.UserId,
                Message = $"Your appointment for {appointment.ServiceType} on {appointment.AppointmentDateTime:g} has been marked as Completed.",
                CreatedAt = DateTime.UtcNow
            });

            var callerId = GetCurrentUserId();
            if (callerId.HasValue && callerId.Value != appointment.UserId)
            {
                var customerName = appointment.User?.FullName ?? "A customer";
                dbContext.Notifications.Add(new Notification
                {
                    UserId = callerId.Value,
                    Message = $"Appointment completed: {customerName}'s appointment for {appointment.ServiceType} was marked as Completed.",
                    CreatedAt = DateTime.UtcNow
                });
            }
            await dbContext.SaveChangesAsync();

            return new ApiResponse<AppointmentResponseDto>(true, "Appointment completed successfully.", ToDto(appointment), null);
        }
    }
}
