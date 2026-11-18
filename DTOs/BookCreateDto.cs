using System.ComponentModel.DataAnnotations;
namespace LibraryManagement.DTOs;

public class BookCreateDto
{
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    [Range(1, int.MaxValue)] public int AuthorId { get; set; }
    [Range(1, int.MaxValue)] public int CategoryId { get; set; }
    [Range(0, int.MaxValue)] public int Quantity { get; set; }
    [Range(0, 9999)] public int PublishedYear { get; set; }
}