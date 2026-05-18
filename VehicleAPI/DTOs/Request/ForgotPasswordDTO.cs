namespace VehicleAPI.DTOs.Request
{
    public class ForgotPasswordDTO
    {
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpDTO
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
    }

    public class ResetPasswordDTO
    {
        public string Email { get; set; } = string.Empty;
        public string OtpCode { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}