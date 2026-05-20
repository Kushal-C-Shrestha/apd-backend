using System.ComponentModel.DataAnnotations;

namespace VehicleAPI.DTOs.Request
{
    public class CreateSaleDTO
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public List<CreateSaleItemDTO> Items { get; set; }

        public decimal Discount { get; set; } = 0;

        [Required]
        public decimal AmountPaid { get; set; }
    }

    public class CreateSaleItemDTO
    {
        [Required]
        public int PartId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }
}