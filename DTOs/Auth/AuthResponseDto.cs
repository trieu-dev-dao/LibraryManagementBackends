namespace LibraryManagement.DTOs;

public class AuthResponseDto 
{ 
    public string Token { get; set; } = string.Empty; 
    public DateTime ExpiresAtUtc { get; set; }
    public string Username { get; set; } = string.Empty; 
    public string Role { get; set; } = string.Empty; 
    public int? MemberId { get; set; }
    public int UserId { get; set; }
}