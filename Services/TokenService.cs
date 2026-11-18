using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LibraryManagement.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
namespace LibraryManagement.Services;

public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    public TokenService(IOptions<JwtSettings> options) => _settings = options.Value;
    public (string Token, DateTime ExpiresAtUtc) GenerateToken(int userId, string username, string role)
    {
        var secret = Encoding.UTF8.GetBytes(_settings.SecretKey);
        if (secret.Length < 32)
            throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 bytes.");
        var now = DateTime.UtcNow; var expires = now.AddMinutes(_settings.ExpirationInMinutes);
        var claims = new[]{
            new Claim("userId",userId.ToString()),
            new Claim(ClaimTypes.Name,username),
            new Claim(ClaimTypes.Role,role),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
        };
        var key = new SymmetricSecurityKey(secret);
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(_settings.Issuer, _settings.Audience, claims, now, expires, creds);
        return (new JwtSecurityTokenHandler().WriteToken(jwt), expires);
    }
}