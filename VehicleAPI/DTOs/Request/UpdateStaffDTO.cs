namespace VehicleAPI.DTOs.Request
{
    public class UpdateStaffDTO
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string StaffRole { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}