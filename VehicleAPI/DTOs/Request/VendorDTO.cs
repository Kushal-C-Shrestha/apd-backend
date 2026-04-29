using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.DTOs.Request
{
    public class CreateVendorDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }
    }

    public class UpdateVendorDTO
    {
        [Required, MaxLength(100)]
        public string Name { get; set; }

        [Required, MaxLength(15)]
        public string Phone { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public string Address { get; set; }
    }
}