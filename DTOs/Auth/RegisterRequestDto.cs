
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class RegisterRequestDto
{
    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [StringLength(50, MinimumLength = 3)]
    [RegularExpression(
        @"^[a-zA-Z0-9_.]+$",
        ErrorMessage = "Username chỉ được chứa chữ cái, số, dấu chấm và dấu gạch dưới."
    )]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }
}
