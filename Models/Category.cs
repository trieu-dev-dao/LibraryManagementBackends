using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Category
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    // Navigation property
    public ICollection<Book> Books { get; set; } = new List<Book>();
}
