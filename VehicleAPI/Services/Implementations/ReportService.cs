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
    public class ReportService : IReportService
    {
        private readonly AppDbContext _context;

        public ReportService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<FinancialReportResponseDTO> GetFinancialReportAsync(string timeframe)
        {
            var salesQuery = _context.Sales.Include(s => s.Credit).AsQueryable();

            var today = DateTime.UtcNow.Date;

            if (!string.IsNullOrEmpty(timeframe))
            {
                var tf = timeframe.ToLower();
                if (tf == "today" || tf == "daily")
                {
                    salesQuery = salesQuery.Where(s => s.CreatedAt.Date == today);
                }
                else if (tf == "this month" || tf == "monthly")
                {
                    salesQuery = salesQuery.Where(s => s.CreatedAt.Month == DateTime.UtcNow.Month && s.CreatedAt.Year == DateTime.UtcNow.Year);
                }
                else if (tf == "this year" || tf == "yearly")
                {
                    salesQuery = salesQuery.Where(s => s.CreatedAt.Year == DateTime.UtcNow.Year);
                }
            }

            var sales = await salesQuery.ToListAsync();

            var parts = await _context.Parts.ToListAsync();

            var totalRevenue = sales.Sum(s => s.FinalAmount);
            var totalDiscounts = sales.Sum(s => s.Discount);
            var pendingCredits = sales.Where(s => s.PaymentStatus != "Paid" && s.Credit != null).Sum(s => s.Credit.AmountDue);
            var totalInvoices = sales.Count;
            var paidCount = sales.Count(s => s.PaymentStatus == "Paid");
            var unpaidCount = sales.Count(s => s.PaymentStatus != "Paid");

            var lowStock = parts.Where(p => p.StockQuantity < 10)
                .Select(p => new LowStockPartDTO { PartId = p.PartId, Name = p.Name, StockQuantity = p.StockQuantity })
                .ToList();

            var topStocked = parts.OrderByDescending(p => p.StockQuantity).Take(5)
                .Select(p => new TopStockedPartDTO { PartId = p.PartId, Name = p.Name, StockQuantity = p.StockQuantity })
                .ToList();

            return new FinancialReportResponseDTO
            {
                TotalRevenue = totalRevenue,
                TotalDiscounts = totalDiscounts,
                PendingCredits = pendingCredits,
                TotalInvoices = totalInvoices,
                PaidSalesCount = paidCount,
                UnpaidSalesCount = unpaidCount,
                LowStockParts = lowStock,
                TopStockedParts = topStocked
            };
        }

        public async Task<CustomerReportsResponseDTO> GetCustomerReportsAsync()
        {
            var sales = await _context.Sales.Include(s => s.User).Include(s => s.Credit).ToListAsync();

            // Group sales by user id
            var grouped = sales.GroupBy(s => s.UserId).ToList();

            var regulars = new List<RegularCustomerDTO>();
            var highSpenders = new List<HighSpenderDTO>();
            var pendingCredits = new List<PendingCreditCustomerDTO>();

            foreach (var group in grouped)
            {
                var userId = group.Key;
                var firstSale = group.FirstOrDefault();
                var customerName = firstSale?.User?.FullName ?? "Walk-in Client";

                var visits = group.Count();
                var totalSpent = group.Sum(s => s.FinalAmount);
                var pendingCreditSum = group.Where(s => s.PaymentStatus != "Paid" && s.Credit != null).Sum(s => s.Credit.AmountDue);

                regulars.Add(new RegularCustomerDTO
                {
                    UserId = userId,
                    CustomerName = customerName,
                    VisitCount = visits
                });

                highSpenders.Add(new HighSpenderDTO
                {
                    UserId = userId,
                    CustomerName = customerName,
                    TotalSpent = totalSpent
                });

                if (pendingCreditSum > 0)
                {
                    pendingCredits.Add(new PendingCreditCustomerDTO
                    {
                        UserId = userId,
                        CustomerName = customerName,
                        PendingCredit = pendingCreditSum
                    });
                }
            }

            return new CustomerReportsResponseDTO
            {
                Regulars = regulars.OrderByDescending(c => c.VisitCount).Take(5).ToList(),
                HighSpenders = highSpenders.OrderByDescending(c => c.TotalSpent).Take(5).ToList(),
                PendingCredits = pendingCredits.OrderByDescending(c => c.PendingCredit).ToList()
            };
        }
    }
}