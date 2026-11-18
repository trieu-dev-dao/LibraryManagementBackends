using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class InventoryVerification
{
    public long Id { get; set; }
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int OutstandingBorrowedCopies { get; set; }
    [Required, StringLength(200)]
    public string Source { get; set; } = string.Empty;
    public DateTime VerifiedAtUtc { get; set; }
}
