using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using VehicleAPI.Data;
using VehicleAPI.Models;

namespace VehicleAPI.Services.Implementations
{
    public class CreditReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<CreditReminderService> _logger;

        public CreditReminderService(IServiceScopeFactory scopeFactory, IConfiguration config, ILogger<CreditReminderService> logger)
        {
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CreditReminderService background task is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckAndSendRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing CreditReminderService.");
                }

                // Run once a day
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private async Task CheckAndSendRemindersAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var oneMonthAgo = DateTime.UtcNow.AddMonths(-1);

            // Fetch credits older than 1 month that are still unpaid
            var overdueCredits = await db.Credits
                .Include(c => c.Sale)
                .ThenInclude(s => s.User)
                .Where(c => !c.IsPaid && c.CreatedAt < oneMonthAgo)
                .ToListAsync(stoppingToken);

            if (!overdueCredits.Any())
                return;

            _logger.LogInformation($"Found {overdueCredits.Count} overdue credits.");

            var host = _config["Email:SmtpHost"];
            var portStr = _config["Email:SmtpPort"];
            var user = _config["Email:SmtpUser"];
            var pass = _config["Email:SmtpPass"];
            var fromName = _config["Email:FromName"] ?? "Vehicle Service Center";

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(portStr) || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                _logger.LogWarning("Email settings are not fully configured. Cannot send reminders.");
                return;
            }

            int port = int.Parse(portStr);

            using var client = new SmtpClient(host, port)
            {
                Credentials = new NetworkCredential(user, pass),
                EnableSsl = true
            };

            foreach (var credit in overdueCredits)
            {
                var customerEmail = credit.Sale?.User?.Email;
                var customerName = credit.Sale?.User?.FullName;

                if (string.IsNullOrEmpty(customerEmail))
                    continue;

                try
                {
                    var mail = new MailMessage
                    {
                        From = new MailAddress(user, fromName),
                        Subject = "Overdue Credit Reminder",
                        IsBodyHtml = true,
                        Body = $@"
                            <div style='font-family: Arial, sans-serif; padding: 20px;'>
                                <h2>Hello {customerName},</h2>
                                <p>This is a friendly reminder that you have an unpaid credit of <strong>${credit.AmountDue}</strong> from your purchase on {credit.Sale?.CreatedAt.ToString("MMM dd, yyyy")}.</p>
                                <p>Please settle your balance as soon as possible.</p>
                                <br>
                                <p>Thank you,</p>
                                <p>{fromName}</p>
                            </div>"
                    };

                    mail.To.Add(customerEmail);
                    await client.SendMailAsync(mail, stoppingToken);
                    _logger.LogInformation($"Sent overdue reminder to {customerEmail} for Credit ID {credit.CreditId}.");

                    // Add notification for the customer
                    db.Notifications.Add(new Notification
                    {
                        UserId = credit.Sale.UserId,
                        Message = $"Friendly reminder: You have an unpaid credit of Rs. {credit.AmountDue:N0} from your purchase on {credit.Sale.CreatedAt:MMM dd, yyyy}.",
                        CreatedAt = DateTime.UtcNow
                    });

                    // Add notification for admin
                    var adminUser = await db.Users.FirstOrDefaultAsync(u => u.RoleId == 1, stoppingToken);
                    if (adminUser != null)
                    {
                        db.Notifications.Add(new Notification
                        {
                            UserId = adminUser.UserId,
                            Message = $"Credit payment reminder sent: {customerName} has an outstanding credit of Rs. {credit.AmountDue:N0}.",
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to send reminder to {customerEmail} for Credit ID {credit.CreditId}.");
                }
            }
        }
    }
}
