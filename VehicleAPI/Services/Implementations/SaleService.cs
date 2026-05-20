using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
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
        private readonly IConfiguration _config;

        public SaleService(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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
                if (part == null || part.IsDeleted)
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
                
                if (part.StockQuantity < 10)
                {
                    var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.RoleId == 1);
                    if (adminUser != null)
                    {
                        var message = $"Low Stock Alert: '{part.Name}' stock is down to {part.StockQuantity}.";
                        _context.Notifications.Add(new Notification
                        {
                            UserId = adminUser.UserId,
                            Message = message,
                            CreatedAt = DateTime.UtcNow
                        });

                        if (!string.IsNullOrEmpty(adminUser.Email))
                        {
                            _ = SendLowStockNotificationEmailAsync(adminUser.Email, adminUser.FullName, part.Name, part.StockQuantity);
                        }
                    }
                }
            }

            sale.TotalAmount = total;

            if (total > 5000)
            {
                decimal loyaltyDiscount = total * 0.10m;
                sale.Discount += loyaltyDiscount;
            }

            sale.FinalAmount = total - sale.Discount;

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

        private async Task SendLowStockNotificationEmailAsync(string toEmail, string fullName, string partName, int remainingStock)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"];
                var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

                var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser!, fromName),
                    Subject = "Low Stock Alert",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:500px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;overflow:hidden;'>
                            <div style='background:#ef4444;padding:24px;text-align:center;'>
                                <h2 style='color:white;margin:0;'>Low Stock Alert</h2>
                            </div>
                            <div style='padding:28px;'>
                                <p style='font-size:16px;'>Hello <strong>{fullName}</strong>,</p>
                                <p>The stock for the following item has fallen below the minimum threshold (10):</p>
                                <div style='background:#f1f5f9;border-radius:6px;padding:16px;margin:16px 0;'>
                                    <p style='margin:4px 0;'><strong>Part Name:</strong> {partName}</p>
                                    <p style='margin:4px 0;color:#ef4444;'><strong>Remaining Stock:</strong> {remainingStock}</p>
                                </div>
                                <p style='color:#6b7280;font-size:13px;'>Please restock this item soon to avoid running out of inventory.</p>
                            </div>
                        </div>"
                };

                mail.To.Add(toEmail);
                await client.SendMailAsync(mail);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Low stock email sending failed: {ex.Message}");
            }
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

        public async Task<bool> SendInvoiceEmailAsync(int saleId)
        {
            var sale = await _context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleItems).ThenInclude(si => si.Part)
                .FirstOrDefaultAsync(s => s.SaleId == saleId);

            if (sale == null || sale.User == null || string.IsNullOrEmpty(sale.User.Email))
                return false;

            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"];
                var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                    return false;

                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(smtpUser, smtpPass),
                    EnableSsl = true
                };

                var itemsHtml = string.Join("", sale.SaleItems.Select(item => $@"
                    <tr>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.Part?.Name ?? "Unknown"}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>{item.Quantity}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>${item.UnitPrice}</td>
                        <td style='padding: 8px; border-bottom: 1px solid #ddd;'>${item.Subtotal}</td>
                    </tr>"));

                var mail = new MailMessage
                {
                    From = new MailAddress(smtpUser, fromName),
                    Subject = $"Invoice for Purchase #{sale.SaleId}",
                    IsBodyHtml = true,
                    Body = $@"
                        <div style='font-family:Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #e5e7eb;padding:20px;'>
                            <h2>Purchase Invoice #{sale.SaleId}</h2>
                            <p><strong>Date:</strong> {sale.CreatedAt:MMM dd, yyyy}</p>
                            <p><strong>Customer:</strong> {sale.User.FullName}</p>
                            
                            <table style='width: 100%; border-collapse: collapse; margin-top: 20px;'>
                                <thead>
                                    <tr style='background-color: #f8f9fa; text-align: left;'>
                                        <th style='padding: 8px; border-bottom: 2px solid #ddd;'>Item</th>
                                        <th style='padding: 8px; border-bottom: 2px solid #ddd;'>Qty</th>
                                        <th style='padding: 8px; border-bottom: 2px solid #ddd;'>Unit Price</th>
                                        <th style='padding: 8px; border-bottom: 2px solid #ddd;'>Subtotal</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {itemsHtml}
                                </tbody>
                            </table>

                            <div style='margin-top: 20px; text-align: right;'>
                                <p><strong>Subtotal:</strong> ${sale.TotalAmount}</p>
                                <p><strong>Discount:</strong> ${sale.Discount}</p>
                                <h3><strong>Total:</strong> ${sale.FinalAmount}</h3>
                                <p><strong>Amount Paid:</strong> ${sale.AmountPaid}</p>
                                <p><strong>Status:</strong> {sale.PaymentStatus}</p>
                            </div>
                            <br>
                            <p>Thank you for your business!</p>
                        </div>"
                };

                mail.To.Add(sale.User.Email);
                await client.SendMailAsync(mail);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Invoice email sending failed: {ex.Message}");
                return false;
            }
        }
    }
}