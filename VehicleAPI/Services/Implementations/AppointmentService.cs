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
                ServiceType = dto.ServiceType,
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
    }
}
