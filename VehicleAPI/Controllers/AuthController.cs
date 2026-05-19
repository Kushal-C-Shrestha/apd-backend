using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VehicleAPI.DTOs.Request;
using VehicleAPI.Services.Interfaces;

namespace VehicleAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        private void SetRefreshTokenCookie(string refreshToken)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7),
                SameSite = SameSiteMode.Lax,
                Secure = false // Set to true in production
            };
            Response.Cookies.Append("refreshToken", refreshToken, cookieOptions);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            try
            {
                var result = await _authService.LoginAsync(dto);
                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(new { success = true, message = "Login successful.", data = result });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("|"))
                {
                    var parts = ex.Message.Split('|', 2);
                    var errors = new Dictionary<string, string> { { parts[0], parts[1] } };
                    return BadRequest(new { success = false, message = parts[1], errors = errors });
                }
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] CustomerSelfRegisterDTO dto)
        {
            try
            {
                var result = await _authService.CustomerSelfRegisterAsync(dto);
                SetRefreshTokenCookie(result.RefreshToken);
                return Ok(new { success = true, message = "Account created successfully.", data = result });
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("|"))
                {
                    var parts = ex.Message.Split('|', 2);
                    var errors = new Dictionary<string, string> { { parts[0], parts[1] } };
                    return BadRequest(new { success = false, message = parts[1], errors = errors });
                }
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO dto)
        {
            try
            {
                await _authService.ForgotPasswordAsync(dto);
                return Ok(new { success = true, message = "OTP sent to your email." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDTO dto)
        {
            try
            {
                await _authService.VerifyOtpAsync(dto);
                return Ok(new { success = true, message = "OTP verified successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
        {
            try
            {
                await _authService.ResetPasswordAsync(dto);
                return Ok(new { success = true, message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [AllowAnonymous]
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDTO? dto)
        {
            try
            {
                var refreshToken = dto?.RefreshToken ?? Request.Cookies["refreshToken"];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { success = false, message = "No refresh token provided." });
                }

                var (newAccessToken, newRefreshToken) = await _authService.RefreshTokenAsync(refreshToken);
                SetRefreshTokenCookie(newRefreshToken);

                return Ok(new { 
                    accessToken = newAccessToken, 
                    refreshToken = newRefreshToken 
                });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
        }
    }
}