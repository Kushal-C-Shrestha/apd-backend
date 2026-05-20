using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
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
        private readonly IConfiguration _config;

        public PartService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        public async Task<List<PartResponseDTO>> GetAllPartsAsync()
        {
            return await _context.Parts
                .Where(p => !p.IsDeleted)
                .OrderBy(p => p.Name)
                .Select(p => MapToPartResponse(p))
                .ToListAsync();
        }

        public async Task<PartResponseDTO?> GetPartByIdAsync(int partId)
        {
            var part = await _context.Parts.FirstOrDefaultAsync(p => p.PartId == partId && !p.IsDeleted);
            return part == null ? null : MapToPartResponse(part);
        }

        public async Task<PartResponseDTO> CreatePartAsync(CreatePartDTO dto)
        {
            var part = new Part
            {
                Name = dto.Name,
                Description = dto.Description,
                CostPrice = dto.CostPrice,
                UnitPrice = dto.UnitPrice,
                StockQuantity = dto.StockQuantity,
                ImageUrl = dto.ImageUrl
            };

            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            return MapToPartResponse(part);
        }

        public async Task<PartResponseDTO?> UpdatePartAsync(int partId, UpdatePartDTO dto)
        {
            var part = await _context.Parts.FirstOrDefaultAsync(p => p.PartId == partId && !p.IsDeleted);
            if (part == null) return null;

            part.Name = dto.Name;
            part.Description = dto.Description;
            part.CostPrice = dto.CostPrice;
            part.UnitPrice = dto.UnitPrice;
            part.StockQuantity = dto.StockQuantity;
            part.ImageUrl = dto.ImageUrl;

            await _context.SaveChangesAsync();
            return MapToPartResponse(part);
        }

        public async Task<bool> DeletePartAsync(int partId)
        {
            var part = await _context.Parts.FindAsync(partId);
            if (part == null || part.IsDeleted) return false;

            part.IsDeleted = true;
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
                PaymentStatus = dto.PaymentStatus,
                PurchaseItems = new List<PurchaseItem>()
            };

            decimal total = 0;

            foreach (var item in dto.Items)
            {
                if (item.PartId <= 0)
                    throw new Exception("Invalid PartId");

                var part = await _context.Parts.FindAsync(item.PartId);

                if (part == null || part.IsDeleted)
                    throw new KeyNotFoundException($"Part with ID {item.PartId} not found.");

                var subtotal = item.Quantity * item.UnitCost;
                total += subtotal;

                purchase.PurchaseItems.Add(new PurchaseItem
                {
                    PartId = part.PartId,
                    Quantity = item.Quantity,
                    UnitCost = item.UnitCost,
                    Subtotal = subtotal
                });

                part.StockQuantity += item.Quantity;
            }

            purchase.TotalAmount = total;

            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
            if (adminUser != null)
            {
                var vendorName = (await _context.Vendors.FindAsync(purchase.VendorId))?.Name ?? "supplier";

                _context.Notifications.Add(new Notification
                {
                    UserId = adminUser.UserId,
                    Message = $"Procurement invoice logged: Recorded Rs. {purchase.TotalAmount:N0} stock purchase from '{vendorName}'.",
                    CreatedAt = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
            }

            var savedPurchase = await _context.Purchases
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Part)
                .FirstOrDefaultAsync(p => p.PurchaseId == purchase.PurchaseId);

            if (savedPurchase?.Vendor != null)
            {
                _ = SendPurchaseEmailToVendorAsync(savedPurchase, savedPurchase.Vendor);
            }

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
                PaymentStatus = p.PaymentStatus,
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
                PaymentStatus = purchase.PaymentStatus,
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
            CostPrice = part.CostPrice,
            UnitPrice = part.UnitPrice,
            StockQuantity = part.StockQuantity,
            CreatedAt = part.CreatedAt,
            ImageUrl = part.ImageUrl
        };

        private async Task<PurchaseResponseDTO> BuildPurchaseResponseAsync(int purchaseId)
        {
            return (await GetPurchaseByIdAsync(purchaseId))!;
        }


        private async Task SendPurchaseEmailToVendorAsync(Purchase purchase, Vendor vendor)
        {
            if (vendor == null || string.IsNullOrEmpty(vendor.Email))
                return;

            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"];
                var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                    return;

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var itemsHtml = string.Join("", purchase.PurchaseItems.Select(item => $@"
                    <tr>
                        <td>{item.Part?.Name ?? "Unknown"}</td>
                        <td>{item.Quantity}</td>
                        <td>Rs. {item.UnitCost:N2}</td>
                        <td>Rs. {item.Subtotal:N2}</td>
                    </tr>"));

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser, fromName),
                    Subject = $"New Purchase Order #{purchase.PurchaseId}",
                    IsBodyHtml = true,
                    Body = $"<html><body>{itemsHtml}</body></html>"
                };

                mail.To.Add(vendor.Email);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email failed: {ex.Message}");
            }
        }
    }
}