using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class AppointmentService(AppDbContext dbContext) : IAppointmentService
    {
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

            var appointment = new Appointment
            {
                UserId = dto.UserId,
                VehicleId = dto.VehicleId,
                AppointmentDateTime = DateTime.SpecifyKind(dto.AppointmentDate.ToDateTime(dto.AppointmentTime), DateTimeKind.Utc),
                ServiceType = string.IsNullOrWhiteSpace(dto.ServiceType) ? "General Service" : dto.ServiceType,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            await dbContext.Appointments.AddAsync(appointment);
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

            appointment.AppointmentDateTime = DateTime.SpecifyKind(dto.AppointmentDate.ToDateTime(dto.AppointmentTime), DateTimeKind.Utc);
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
            return new ApiResponse<AppointmentResponseDto>(true, "Appointment cancelled successfully.", ToDto(appointment), null);
        }
    }
}
