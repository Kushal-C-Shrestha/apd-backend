using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; }
        public ICollection<User> Users { get; set; } = new List<User>();

    }
}
