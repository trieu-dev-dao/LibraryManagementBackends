
using LibraryManagement.DTOs;

namespace LibraryManagement.Services;

public interface IAuthService
{
    Task<(bool Success, string Message)> RegisterAsync(
        RegisterRequestDto dto);

    Task<AuthResponseDto?> LoginAsync(
        LoginRequestDto dto);

    Task<AuthResponseDto?> GoogleLoginAsync(
        GoogleLoginDto dto);
}
