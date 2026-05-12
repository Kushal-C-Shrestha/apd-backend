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

            return new SaleResponseDTO
{
    SaleId = sale.SaleId,
    UserId = sale.UserId,
    Discount = sale.Discount,
    TotalAmount = sale.TotalAmount,
    FinalAmount = sale.FinalAmount,
    AmountPaid = sale.AmountPaid,
    PaymentStatus = sale.PaymentStatus,
    SaleDate = sale.SaleDate,
    Items = sale.SaleItems.Select(si => new SaleItemResponseDTO
    {
        PartId = si.PartId,
        Quantity = si.Quantity,
        UnitPrice = si.UnitPrice,
        Subtotal = si.Subtotal
    }).ToList()
};
        }
    }
}