using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class PartRequest
    {
        [Key]
        public int RequestId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int PartId { get; set; }

        [Required]
        public int Quantity { get; set; }

        public string Status { get; set; } = "Pending"; 

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; }
        public Part Part { get; set; }


    }
}
