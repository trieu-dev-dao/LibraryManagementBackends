
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Vui lòng nhập username hoặc email.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;
}
