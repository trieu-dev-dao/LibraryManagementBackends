using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.DTOs;

public class CategoryCreateDto
{
    [Required, StringLength(100)] 
    public string Name { get; set; } = string.Empty;
}