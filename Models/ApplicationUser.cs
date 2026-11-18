using Microsoft.AspNetCore.Identity;

namespace LibraryManagement.Models;

public class ApplicationUser : IdentityUser<int>
{
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = "User";
    public Member? Member { get; set; }
}