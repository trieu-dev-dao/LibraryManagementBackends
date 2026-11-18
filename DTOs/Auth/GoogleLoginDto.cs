using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class GoogleLoginDto
{
    [Required(ErrorMessage = "IdToken không được để trống.")]
    public string IdToken { get; set; } = string.Empty;
}