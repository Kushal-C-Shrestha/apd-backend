using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class SaleService : ISaleService
    {
        private readonly AppDbContext _context;

        public SaleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SaleResponseDTO> CreateSaleAsync(CreateSaleDTO dto)
        {
            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
                throw new KeyNotFoundException($"User with ID {dto.UserId} not found.");

            if (dto.Items == null || dto.Items.Count == 0)
                throw new InvalidOperationException("Sale must have at least one item.");

            var sale = new Sale
            {
                UserId = dto.UserId,
                Discount = dto.Discount,
                AmountPaid = dto.AmountPaid,
                SaleItems = new List<SaleItem>()
            };

            decimal total = 0;

            foreach (var item in dto.Items)
            {
                var part = await _context.Parts.FindAsync(item.PartId);
                if (part == null)
                    throw new KeyNotFoundException($"Part with ID {item.PartId} not found.");

                if (part.StockQuantity < item.Quantity)
                    throw new InvalidOperationException($"Insufficient stock for part '{part.Name}'. Available: {part.StockQuantity}, Requested: {item.Quantity}.");

                var subtotal = item.Quantity * part.UnitPrice;
                total += subtotal;

                sale.SaleItems.Add(new SaleItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitPrice = part.UnitPrice,
                    Subtotal = subtotal
                });

                part.StockQuantity -= item.Quantity;
            }

            sale.TotalAmount = total;
            sale.FinalAmount = total - dto.Discount;

            if (dto.AmountPaid >= sale.FinalAmount)
            {
                sale.PaymentStatus = "Paid";
            }
            else if (dto.AmountPaid > 0)
            {
                sale.PaymentStatus = "Partial";
            }
            else
            {
                sale.PaymentStatus = "Unpaid";
            }

            _context.Sales.Add(sale);
            await _context.SaveChangesAsync();

            if (sale.PaymentStatus != "Paid")
            {
                var credit = new Credit
                {
                    SaleId = sale.SaleId,
                    AmountDue = sale.FinalAmount - dto.AmountPaid,
                    IsPaid = false
                };
                _context.Credits.Add(credit);
                await _context.SaveChangesAsync();
            }

            return (await GetSaleByIdAsync(sale.SaleId))!;
        }

        public async Task<List<SaleResponseDTO>> GetAllSalesAsync()
        {
            var sales = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems).ThenInclude(si => si.Part)
                .Include(s => s.Credit)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return sales.Select(MapToResponse).ToList();
        }

        public async Task<SaleResponseDTO?> GetSaleByIdAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems).ThenInclude(si => si.Part)
                .Include(s => s.Credit)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            return sale == null ? null : MapToResponse(sale);
        }

        public async Task<List<SaleResponseDTO>> GetSalesByUserIdAsync(int userId)
        {
            var sales = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems).ThenInclude(si => si.Part)
                .Include(s => s.Credit)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return sales.Select(MapToResponse).ToList();
        }

        public async Task<SaleResponseDTO?> SettleCreditAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.Credit)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null) return null;

            if (sale.Credit == null || sale.Credit.IsPaid)
                throw new InvalidOperationException("No outstanding credit for this sale.");

            sale.Credit.IsPaid = true;
            sale.AmountPaid = sale.FinalAmount;
            sale.PaymentStatus = "Paid";

            await _context.SaveChangesAsync();

            return (await GetSaleByIdAsync(saleId))!;
        }

        private static SaleResponseDTO MapToResponse(Sale sale) => new()
        {
            SaleId = sale.SaleId,
            UserId = sale.UserId,
            UserName = sale.User?.FullName ?? "",
            TotalAmount = sale.TotalAmount,
            Discount = sale.Discount,
            FinalAmount = sale.FinalAmount,
            AmountPaid = sale.AmountPaid,
            PaymentStatus = sale.PaymentStatus,
            CreatedAt = sale.CreatedAt,
            Items = sale.SaleItems.Select(si => new SaleItemResponseDTO
            {
                SaleItemId = si.SaleItemId,
                PartId = si.PartId,
                PartName = si.Part?.Name ?? "",
                Quantity = si.Quantity,
                UnitPrice = si.UnitPrice,
                Subtotal = si.Subtotal
            }).ToList(),
            Credit = sale.Credit == null ? null : new CreditResponseDTO
            {
                CreditId = sale.Credit.CreditId,
                AmountDue = sale.Credit.AmountDue,
                IsPaid = sale.Credit.IsPaid,
                CreatedAt = sale.Credit.CreatedAt
            }
        };
    }
}