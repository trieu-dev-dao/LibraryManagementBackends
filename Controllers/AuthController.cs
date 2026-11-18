
using Google.Apis.Auth;
using System.IdentityModel.Tokens.Jwt;
using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _authService = authService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);

        if (!result.Success)
        {
            return Conflict(new { message = result.Message });
        }

        return Ok(new { message = result.Message });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(
        [FromBody] LoginRequestDto dto)
    {
        var response = await _authService.LoginAsync(dto);

        if (response == null)
        {
            return Unauthorized(new
            {
                message = "Tài khoản hoặc mật khẩu không đúng."
            });
        }

        return Ok(response);
    }

    [HttpPost("google-login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> GoogleLogin(
        [FromBody] GoogleLoginDto dto)
    {
        try
        {
            var response = await _authService.GoogleLoginAsync(dto);

            if (response == null)
            {
                return Unauthorized(new
                {
                    code = "google_email_unverified",
                    message = "Email của tài khoản Google chưa được xác minh."
                });
            }

            return Ok(response);
        }
        catch (InvalidJwtException)
        {
            LogRejectedGoogleToken(dto.IdToken);
            return Unauthorized(new
            {
                code = "google_token_invalid",
                message = "Thông tin đăng nhập Google không hợp lệ hoặc đã hết hạn. Vui lòng thử lại."
            });
        }
        catch (InvalidOperationException)
        {
            return StatusCode(500, new
            {
                message = "Máy chủ chưa được cấu hình Google Client ID."
            });
        }
        catch (Exception e)
        {
            _logger.LogError("Google login failed after token validation. FailureType={FailureType}", e.GetType().Name);
            return StatusCode(500, new
            {
                code = "google_login_failed",
                message = "Máy chủ không hoàn tất được đăng nhập Google. Vui lòng thử lại."
            });
        }
    }

    private void LogRejectedGoogleToken(string rawToken)
    {
        var now = DateTimeOffset.UtcNow;
        try
        {
            // These unverified metadata values are diagnostics only; ValidateAsync remains authoritative.
            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(rawToken);
            var epoch = now.ToUnixTimeSeconds();
            _logger.LogWarning(
                "Google token rejected. ServerUtc={ServerUtc} AudienceMatches={AudienceMatches} IssuedInFuture={IssuedInFuture} NotYetValid={NotYetValid} Expired={Expired}",
                now, jwt.Audiences.Contains(_configuration["Authentication:Google:ClientId"]),
                jwt.Payload.IssuedAt == DateTime.MinValue ? null : jwt.Payload.IssuedAt > now.UtcDateTime,
                jwt.Payload.NotBefore is long nbf ? nbf > epoch : null,
                jwt.Payload.Expiration is long exp ? exp <= epoch : null);
        }
        catch (Exception)
        {
            _logger.LogWarning("Google token rejected. ServerUtc={ServerUtc} MetadataReadable=false", now);
        }
    }
}
