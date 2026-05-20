namespace VehicleAPI.DTOs.Request
{
    public class CreateReviewDTO
    {
        public int UserId { get; set; }
        public int AppointmentId { get; set; }

        /// <summary>1–5</summary>
        public int Rating { get; set; }

        public string? Comment { get; set; }
    }
}
