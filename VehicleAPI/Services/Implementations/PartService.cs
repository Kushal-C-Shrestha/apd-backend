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
                Part part;
                if (item.PartId.HasValue && item.PartId.Value > 0)
                {
                    part = await _context.Parts.FindAsync(item.PartId.Value);
                    if (part == null || part.IsDeleted)
                        throw new KeyNotFoundException($"Part with ID {item.PartId} not found.");

                    // Calculate Weighted Average Cost (WAC) for existing part
                    int oldQty = Math.Max(0, part.StockQuantity);
                    decimal oldCost = part.CostPrice;
                    decimal totalOldValue = oldQty * oldCost;
                    decimal totalNewValue = item.Quantity * item.UnitCost;
                    int newQty = oldQty + item.Quantity;

                    decimal newCostPrice = (totalOldValue + totalNewValue) / newQty;

                    if (part.CostPrice > 0)
                    {
                        decimal markupRatio = part.UnitPrice / part.CostPrice;
                        part.UnitPrice = Math.Round(newCostPrice * markupRatio, 2);
                    }
                    else
                    {
                        part.UnitPrice = item.UnitPrice ?? Math.Round(newCostPrice * 1.2m, 2);
                    }

                    part.CostPrice = newCostPrice;
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(item.PartName))
                        throw new ArgumentException("Part name is required for new parts.");

                    part = new Part
                    {
                        Name = item.PartName,
                        Description = string.Empty,
                        CostPrice = item.UnitCost,
                        UnitPrice = item.UnitPrice ?? (item.UnitCost * 1.2m),
                        StockQuantity = 0,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Parts.Add(part);
                    await _context.SaveChangesAsync();
                }

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

            // Add notification for admin
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

            // Fetch fully loaded purchase including vendor & part items to send an email notification to vendor
            var savedPurchase = await _context.Purchases
                .Include(p => p.Vendor)
                .Include(p => p.PurchaseItems)
                    .ThenInclude(pi => pi.Part)
                .FirstOrDefaultAsync(p => p.PurchaseId == purchase.PurchaseId);

            if (savedPurchase != null && savedPurchase.Vendor != null)
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
                        <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: left;'>{item.Part?.Name ?? "Unknown"}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: center;'>{item.Quantity}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>Rs. {item.UnitCost:N2}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd; text-align: right;'>Rs. {item.Subtotal:N2}</td>
                    </tr>"));

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser, fromName),
                    Subject = $"New Purchase Order #{purchase.PurchaseId} - {fromName}",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e5e7eb;padding:20px;border-radius:8px;box-shadow:0 2px 4px rgba(0,0,0,0.05);'>
                            <div style='background-color:#2563eb;color:white;padding:20px;text-align:center;border-radius:6px 6px 0 0;'>
                                <h2 style='margin:0;font-size:22px;'>Purchase Order #{purchase.PurchaseId}</h2>
                                <p style='margin:5px 0 0 0;font-size:14px;opacity:0.9;'>From: {fromName}</p>
                            </div>
                            
                            <div style='padding:20px 0;'>
                                <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px;'>
                                    <tr>
                                        <td style='vertical-align:top; width:50%;'>
                                            <p style='margin:0;font-size:12px;color:#6b7280;text-transform:uppercase;font-weight:bold;'>Vendor Details</p>
                                            <p style='margin:5px 0;font-size:14px;font-weight:bold;color:#111827;'>{vendor.Name}</p>
                                            <p style='margin:3px 0;font-size:13px;color:#4b5563;'>Contact: {vendor.ContactPerson}</p>
                                            <p style='margin:3px 0;font-size:13px;color:#4b5563;'>Phone: {vendor.Phone}</p>
                                        </td>
                                        <td style='vertical-align:top; width:50%; text-align:right;'>
                                            <p style='margin:0;font-size:12px;color:#6b7280;text-transform:uppercase;font-weight:bold;'>Order Information</p>
                                            <p style='margin:5px 0;font-size:13px;color:#4b5563;'><strong>Date:</strong> {purchase.CreatedAt:MMM dd, yyyy}</p>
                                            <p style='margin:3px 0;font-size:13px;color:#4b5563;'><strong>Payment Status:</strong> <span style='background-color:#dbeafe;color:#1e40af;padding:2px 6px;border-radius:4px;font-size:11px;font-weight:bold;'>{purchase.PaymentStatus}</span></p>
                                        </td>
                                    </tr>
                                </table>

                                <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                                    <thead>
                                        <tr style='background-color: #f3f4f6; text-align: left;'>
                                            <th style='padding: 10px; border-bottom: 2px solid #e5e7eb; font-size:12px; color:#4b5563; font-weight:bold;'>Item / Part Name</th>
                                            <th style='padding: 10px; border-bottom: 2px solid #e5e7eb; font-size:12px; color:#4b5563; font-weight:bold; text-align:center;'>Qty</th>
                                            <th style='padding: 10px; border-bottom: 2px solid #e5e7eb; font-size:12px; color:#4b5563; font-weight:bold; text-align:right;'>Unit Cost</th>
                                            <th style='padding: 10px; border-bottom: 2px solid #e5e7eb; font-size:12px; color:#4b5563; font-weight:bold; text-align:right;'>Subtotal</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        {itemsHtml}
                                    </tbody>
                                </table>

                                <div style='margin-top: 20px; text-align: right; border-top:2px solid #e5e7eb; padding-top:15px;'>
                                    <p style='margin:0;font-size:14px;color:#4b5563;'><strong>Subtotal:</strong> Rs. {purchase.TotalAmount:N2}</p>
                                    <h3 style='margin:5px 0 0 0;font-size:18px;color:#111827;'><strong>Total Amount:</strong> Rs. {purchase.TotalAmount:N2}</h3>
                                </div>
                            </div>
                            
                            <div style='background-color:#f9fafb;padding:15px;text-align:center;border-radius:6px;font-size:12px;color:#6b7280;border-top:1px solid #e5e7eb;'>
                                This is an automated email from {fromName}. Please review the order. For any queries, contact our admin panel.
                            </div>
                        </div>"
                };

                mail.To.Add(vendor.Email);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Purchase order email sending to vendor failed: {ex.Message}");
            }
        }

        private async Task<PurchaseResponseDTO> BuildPurchaseResponseAsync(int purchaseId)
        {
            return (await GetPurchaseByIdAsync(purchaseId))!;
        }
    }
}