using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VehicleAPI.Data;
using VehicleAPI.DTOs.Response;
using VehicleAPI.Models;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;

        public NotificationService(AppDbContext context)
        {
            _context = context;
        }

        // Converts a UTC DateTime to a human-friendly relative time string
        private static string RelativeTime(DateTime utc)
        {
            var diff = DateTime.UtcNow - utc;
            if (diff.TotalMinutes < 1)    return "Just now";
            if (diff.TotalMinutes < 60)   return $"{(int)diff.TotalMinutes} min ago";
            if (diff.TotalHours < 24)     return $"{(int)diff.TotalHours} hour{((int)diff.TotalHours == 1 ? "" : "s")} ago";
            if (diff.TotalDays < 2)       return "Yesterday";
            if (diff.TotalDays < 7)       return $"{(int)diff.TotalDays} days ago";
            if (diff.TotalDays < 30)      return $"{(int)(diff.TotalDays / 7)} week{((int)(diff.TotalDays / 7) == 1 ? "" : "s")} ago";
            return utc.ToString("d MMM yyyy");
        }

        private static NotificationDTO MapToDTO(Notification n)
        {
            var type = "general";
            var title = "System Notification";

            var msg = n.Message.ToLower();
            if (msg.Contains("stock") || msg.Contains("part"))
            {
                type = "inventory";
                title = "Inventory Alert";
            }
            else if (msg.Contains("credit") || msg.Contains("unpaid") || msg.Contains("payment"))
            {
                type = "payment";
                title = "Payment Update";
            }
            else if (msg.Contains("appointment") || msg.Contains("book"))
            {
                type = "appointment";
                title = "Appointment Update";
            }
            else if (msg.Contains("review") || msg.Contains("experience") || msg.Contains("rating"))
            {
                type = "review";
                title = "Service Feedback";
            }
            else if (msg.Contains("purchase"))
            {
                type = "purchase";
                title = "Purchase Recorded";
            }
            else if (msg.Contains("vehicle"))
            {
                type = "vehicle";
                title = "Vehicle Update";
            }

            return new NotificationDTO
            {
                Id = n.NotificationId.ToString(),
                Type = type,
                Title = title,
                Message = n.Message,
                Time = RelativeTime(n.CreatedAt),
                Unread = !n.IsRead
            };
        }

        public async Task<NotificationsResponseDTO> GetAdminNotificationsAsync()
        {
            var notificationsList = await _context.Notifications
                .Include(n => n.User)
                .Where(n => n.User != null && n.User.RoleId == 1) // Admin recipient
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dtos = notificationsList.Select(MapToDTO).ToList();

            return new NotificationsResponseDTO
            {
                Notifications = dtos,
                UnreadCount = dtos.Count(d => d.Unread)
            };
        }

        public async Task<NotificationsResponseDTO> GetStaffNotificationsAsync()
        {
            var notificationsList = await _context.Notifications
                .Include(n => n.User)
                .Where(n => n.User != null && n.User.RoleId == 2) // Staff recipient
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dtos = notificationsList.Select(MapToDTO).ToList();

            return new NotificationsResponseDTO
            {
                Notifications = dtos,
                UnreadCount = dtos.Count(d => d.Unread)
            };
        }

        public async Task<NotificationsResponseDTO> GetCustomerNotificationsAsync(int userId)
        {
            var notificationsList = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            var dtos = notificationsList.Select(MapToDTO).ToList();

            return new NotificationsResponseDTO
            {
                Notifications = dtos,
                UnreadCount = dtos.Count(d => d.Unread)
            };
        }
    }
}