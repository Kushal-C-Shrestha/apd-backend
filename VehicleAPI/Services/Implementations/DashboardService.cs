using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;

        public DashboardService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<AdminDashboardDTO> GetAdminDashboardAsync()
        {
            var totalRevenue = await _context.Sales.SumAsync(s => s.FinalAmount);
            var partsInStock = await _context.Parts.Where(p => !p.IsDeleted).SumAsync(p => p.StockQuantity);
            var totalStaff = await _context.Users.CountAsync(u => u.RoleId == 2);
            var pendingCredits = await _context.Credits.Where(c => !c.IsPaid).SumAsync(c => c.AmountDue);

            var recentSalesQuery = await _context.Sales
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new SaleResponseDTO
                {
                    SaleId = s.SaleId,
                    UserId = s.UserId,
                    UserName = s.User.FullName,
                    FinalAmount = s.FinalAmount,
                    PaymentStatus = s.PaymentStatus,
                    CreatedAt = s.CreatedAt
                }).ToListAsync();

            var lowStockParts = await _context.Parts
                .Where(p => !p.IsDeleted && p.StockQuantity < 10)
                .Select(p => new PartResponseDTO
                {
                    PartId = p.PartId,
                    Name = p.Name,
                    StockQuantity = p.StockQuantity
                }).ToListAsync();

            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-6 + i)).ToList();
            var minDate = last7Days.First();

            var recentWeekSales = await _context.Sales
                .Where(s => s.CreatedAt >= minDate)
                .Select(s => new { s.CreatedAt, s.FinalAmount })
                .ToListAsync();

            var chartData = last7Days.Select(date => 
                recentWeekSales.Where(s => s.CreatedAt.Date == date).Sum(s => s.FinalAmount)
            ).ToList();

            return new AdminDashboardDTO
            {
                TotalRevenue = totalRevenue,
                PartsInStock = partsInStock,
                TotalStaff = totalStaff,
                PendingCredits = pendingCredits,
                RecentSales = recentSalesQuery,
                LowStockParts = lowStockParts,
                RevenueChartData = chartData
            };
        }

        public async Task<StaffDashboardDTO> GetStaffDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);

            var totalSalesSum = await _context.Sales.SumAsync(s => s.FinalAmount);
            var todaySalesSum = await _context.Sales
                .Where(s => s.CreatedAt >= today && s.CreatedAt < tomorrow)
                .SumAsync(s => s.FinalAmount);

            var totalCustomers = await _context.Users.CountAsync(u => u.RoleId == 3);

            var pendingCredits = await _context.Credits.Where(c => !c.IsPaid).SumAsync(c => c.AmountDue);

            var todayAppointments = await _context.Appointments
                .CountAsync(a => a.AppointmentDateTime >= today && a.AppointmentDateTime < tomorrow);

            var recentSalesQuery = await _context.Sales
                .Include(s => s.User)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Select(s => new SaleResponseDTO
                {
                    SaleId = s.SaleId,
                    UserId = s.UserId,
                    UserName = s.User.FullName,
                    FinalAmount = s.FinalAmount,
                    PaymentStatus = s.PaymentStatus,
                    CreatedAt = s.CreatedAt
                }).ToListAsync();

            var allAppointments = await _context.Appointments.Select(a => a.Status).ToListAsync();
            var statusCounts = new Dictionary<string, int>
            {
                { "Pending", allAppointments.Count(s => s == "Pending") },
                { "Confirmed", allAppointments.Count(s => s == "Confirmed") },
                { "Completed", allAppointments.Count(s => s == "Completed") },
                { "Cancelled", allAppointments.Count(s => s == "Cancelled") }
            };

            var last7Days = Enumerable.Range(0, 7).Select(i => DateTime.UtcNow.Date.AddDays(-6 + i)).ToList();
            var minDate = last7Days.First();

            var recentWeekSales = await _context.Sales
                .Where(s => s.CreatedAt >= minDate)
                .Select(s => new { s.CreatedAt, s.FinalAmount })
                .ToListAsync();

            var chartData = last7Days.Select(date => 
                recentWeekSales.Where(s => s.CreatedAt.Date == date).Sum(s => s.FinalAmount)
            ).ToList();

            return new StaffDashboardDTO
            {
                TodaySalesSum = todaySalesSum,
                TotalSalesSum = totalSalesSum,
                TotalCustomersCount = totalCustomers,
                PendingCreditsSum = pendingCredits,
                TodayAppointmentsCount = todayAppointments,
                RecentSales = recentSalesQuery,
                AppointmentStatusCounts = statusCounts,
                RevenueChartData = chartData
            };
        }
    }
}