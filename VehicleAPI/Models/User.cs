using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleAPI.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required, MaxLength(100)]
        public string FullName { get; set; }

        public string CustomerId { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public Role Role { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();

        public ICollection<Sale> Sales { get; set; } = new List<Sale>();

        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<Appointment> Appointments{ get; set; } = new List<Appointment>();

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

        public ICollection<PartRequest> PartRequests { get; set; } = new List<PartRequest>();
    }
}
