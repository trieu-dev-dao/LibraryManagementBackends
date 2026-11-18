using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Author
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string? Bio { get; set; }

    // Navigation property
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
