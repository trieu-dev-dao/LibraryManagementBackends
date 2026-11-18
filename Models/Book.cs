using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

public class Book
{
    public int Id { get; set; }

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public int AuthorId { get; set; }

    public int CategoryId { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantity { get; set; }

    // Null for legacy rows until a physical inventory reconciliation is completed.
    [Range(0, int.MaxValue)]
    public int? TotalQuantity { get; set; }

    [StringLength(200)]
    public string? TotalQuantitySource { get; set; }

    public DateTime? TotalQuantityVerifiedAtUtc { get; set; }

    [Range(0, 9999)]
    public int PublishedYear { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AuthorId))]
    public Author? Author { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
