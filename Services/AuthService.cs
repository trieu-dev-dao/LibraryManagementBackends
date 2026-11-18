
using Google.Apis.Auth;
using LibraryManagement.Data;
using LibraryManagement.DTOs;
using LibraryManagement.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokens;
    private readonly IPasswordHasher<ApplicationUser> _hasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        ApplicationDbContext db,
        ITokenService tokens,
        IPasswordHasher<ApplicationUser> hasher,
        IConfiguration configuration)
    {
        _db = db;
        _tokens = tokens;
        _hasher = hasher;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message)> RegisterAsync(
    RegisterRequestDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        // Kiểm tra Username đã tồn tại.
        var usernameExists = await _db.Users
            .AnyAsync(x => x.UserName != null &&
                           x.UserName.ToLower() == username.ToLower());

        if (usernameExists)
        {
            return (false, "Username này đã được sử dụng.");
        }

        // Kiểm tra Email đã tồn tại.
        var emailExists = await _db.Users
            .AnyAsync(x => x.Email != null &&
                           x.Email.ToLower() == email);

        if (emailExists)
        {
            return (false, "Email này đã được đăng ký.");
        }

        var user = new ApplicationUser
        {
            UserName = username,
            Email = email,
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone?.Trim(),
            Role = "User"
        };

        user.PasswordHash = _hasher.HashPassword(
            user,
            dto.Password
        );

        await using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var member = new Member
            {
                FullName = user.FullName,
                Email = user.Email!,
                Phone = user.Phone,
                UserId = user.Id
            };

            _db.Members.Add(member);
            await _db.SaveChangesAsync();

            await transaction.CommitAsync();

            return (true, "Đăng ký thành công. Hãy đăng nhập.");
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto dto)
    {
        var login = dto.Username.Trim();

        // Cho phép đăng nhập bằng Username hoặc Email.
        var user = await _db.Users.FirstOrDefaultAsync(x =>
            (x.UserName != null &&
             x.UserName.ToLower() == login.ToLower()) ||
            (x.Email != null &&
             x.Email.ToLower() == login.ToLower()));

        // Không tồn tại tài khoản hoặc tài khoản Google
        // không có mật khẩu nội bộ.
        if (user == null ||
            string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (member == null && user.Role == "User")
        {
            member = new Member
            {
                FullName = user.FullName,
                Email = user.Email!,
                Phone = user.Phone,
                UserId = user.Id
            };

            _db.Members.Add(member);
            await _db.SaveChangesAsync();
        }

        return CreateAuthResponse(user, member?.Id);
    }

    public async Task<AuthResponseDto?> GoogleLoginAsync(
        GoogleLoginDto dto)
    {
        var clientId = _configuration[
            "Authentication:Google:ClientId"
        ];

        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "Chưa cấu hình Google Client ID."
            );
        }

        var settings = new GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new List<string> { clientId }
        };

        // Google xác minh chữ ký, audience và thời hạn token.
        var payload = await GoogleJsonWebSignature.ValidateAsync(
            dto.IdToken,
            settings
        );

        // Chỉ chấp nhận email đã được Google xác minh.
        if (string.IsNullOrWhiteSpace(payload.Email) ||
            payload.EmailVerified != true)
        {
            return null;
        }

        var email = payload.Email.Trim().ToLowerInvariant();

        var user = await _db.Users.FirstOrDefaultAsync(x =>
            x.Email != null &&
            x.Email.ToLower() == email);

        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = string.IsNullOrWhiteSpace(payload.Name)
                    ? email
                    : payload.Name,
                Role = "User",
                PasswordHash = string.Empty
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        // Bảo đảm tài khoản có hồ sơ Member
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.UserId == user.Id);

        if (member == null && user.Role == "User")
        {
            member = new Member
            {
                FullName = user.FullName,
                Email = user.Email!,
                Phone = user.Phone,
                UserId = user.Id
            };

            _db.Members.Add(member);
            await _db.SaveChangesAsync();
        }

        // Không thay đổi Role của tài khoản đã tồn tại
        return CreateAuthResponse(user, member?.Id);
    }

    private AuthResponseDto CreateAuthResponse(
        ApplicationUser user,
        int? memberId = null)
    {
        var (token, expiry) = _tokens.GenerateToken(
            user.Id,
            user.UserName ?? user.Email ?? string.Empty,
            user.Role
        );

        return new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            MemberId = memberId,
            Username = user.UserName ?? user.Email ?? string.Empty,
            Role = user.Role
        };
    }
}
