using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.DTOs;

public class MemberUpdateDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }
}