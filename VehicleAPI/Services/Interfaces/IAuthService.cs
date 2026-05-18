using VehicleAPI.DTOs.Request;
using VehicleAPI.DTOs.Response;

namespace VehicleAPI.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDTO> LoginAsync(LoginDTO dto);
        Task<LoginResponseDTO> CustomerSelfRegisterAsync(CustomerSelfRegisterDTO dto);
        Task ForgotPasswordAsync(ForgotPasswordDTO dto);
        Task VerifyOtpAsync(VerifyOtpDTO dto);
        Task ResetPasswordAsync(ResetPasswordDTO dto);
    }
}