using Microsoft.EntityFrameworkCore;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class PartService : IPartService
    {
        private readonly AppDbContext _context;

        public PartService(AppDbContext context)
        {
            _context = context;
        }


        public async Task<List<PartResponseDTO>> GetAllPartsAsync()
        {
            return await _context.Parts
                .OrderBy(p => p.Name)
                .Select(p => MapToPartResponse(p))
                .ToListAsync();
        }

        public async Task<PartResponseDTO?> GetPartByIdAsync(int partId)
        {
            var part = await _context.Parts.FindAsync(partId);
            return part == null ? null : MapToPartResponse(part);
        }

        public async Task<PartResponseDTO> CreatePartAsync(CreatePartDTO dto)
        {
            var part = new Part
            {
                Name = dto.Name,
                Description = dto.Description,
                UnitPrice = dto.UnitPrice,
                StockQuantity = dto.StockQuantity
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            return MapToPartResponse(part);
        }

        public async Task<PartResponseDTO?> UpdatePartAsync(int partId, UpdatePartDTO dto)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null) return null;

            part.Name = dto.Name;
            part.Description = dto.Description;
            part.UnitPrice = dto.UnitPrice;
            part.StockQuantity = dto.StockQuantity;

            await _context.SaveChangesAsync();
            return MapToPartResponse(part);
        }

        public async Task<bool> DeletePartAsync(int partId)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null) return false;

            bool isReferenced = await _context.PurchaseItems.AnyAsync(pi => pi.PartId == partId)
                             || await _context.SaleItems.AnyAsync(si => si.PartId == partId);

            if (isReferenced)
                throw new InvalidOperationException("Cannot delete a part that has existing purchase or sale records.");

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return true;
        }


        public async Task<PurchaseResponseDTO> CreatePurchaseAsync(CreatePurchaseDTO dto)
        {
            var vendor = await _context.Vendors.FindAsync(dto.VendorId);
            if (vendor == null)
                throw new KeyNotFoundException($"Vendor with ID {dto.VendorId} not found.");

            var purchase = new Purchase
            {
                VendorId = dto.VendorId,
                PurchaseItems = new List<PurchaseItem>()
            };

            decimal total = 0;

            foreach (var item in dto.Items)
            {
                var part = await _context.Parts.FindAsync(item.PartId);
                if (part == null)
                    throw new KeyNotFoundException($"Part with ID {item.PartId} not found.");

                var subtotal = item.Quantity * item.UnitCost;
                total += subtotal;

                purchase.PurchaseItems.Add(new PurchaseItem
                {
                    PartId = item.PartId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Subtotal = subtotal
                });

                part.StockQuantity += item.Quantity;
            }

            purchase.TotalAmount = total;

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            return await BuildPurchaseResponseAsync(purchase.PurchaseId);
        }

        public async Task<List<PurchaseResponseDTO>> GetAllPurchasesAsync()
        {
            var purchases = await _context.Purchases
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Part)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return purchases.Select(p => new PurchaseResponseDTO
            {
                PurchaseId = p.PurchaseId,
                VendorId = p.VendorId,
                VendorName = p.Vendor.Name,
                TotalAmount = p.TotalAmount,
                CreatedAt = p.CreatedAt,
                Items = p.PurchaseItems.Select(pi => new PurchaseItemResponseDTO
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    PartId = pi.PartId,
                    PartName = pi.Part.Name,
                    Quantity = pi.Quantity,
                    UnitCost = pi.UnitCost,
                    Subtotal = pi.Subtotal
                }).ToList()
            }).ToList();
        }

        public async Task<PurchaseResponseDTO?> GetPurchaseByIdAsync(int purchaseId)
        {
            var purchase = await _context.Purchases
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Part)
                .FirstOrDefaultAsync(p => p.PurchaseId == purchaseId);

            if (purchase == null) return null;

            return new PurchaseResponseDTO
            {
                PurchaseId = purchase.PurchaseId,
                VendorId = purchase.VendorId,
                VendorName = purchase.Vendor.Name,
                TotalAmount = purchase.TotalAmount,
                CreatedAt = purchase.CreatedAt,
                Items = purchase.PurchaseItems.Select(pi => new PurchaseItemResponseDTO
                {
                    PurchaseItemId = pi.PurchaseItemId,
                    PartId = pi.PartId,
                    PartName = pi.Part.Name,
                    Quantity = pi.Quantity,
                    UnitCost = pi.UnitCost,
                    Subtotal = pi.Subtotal
                }).ToList()
            };
        }


        private static PartResponseDTO MapToPartResponse(Part part) => new()
        {
            PartId = part.PartId,
            Name = part.Name,
            Description = part.Description,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity,
            CreatedAt = part.CreatedAt
        };

        private async Task<PurchaseResponseDTO> BuildPurchaseResponseAsync(int purchaseId)
        {
            return (await GetPurchaseByIdAsync(purchaseId))!;
        }
    }
}