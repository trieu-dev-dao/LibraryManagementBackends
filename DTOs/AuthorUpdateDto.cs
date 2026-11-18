using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.DTOs;

public class AuthorUpdateDto
{
    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string? Bio { get; set; }
}