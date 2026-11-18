using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models;

public class Member
{
    public int Id { get; set; }

    [Required]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Phone]
    [StringLength(20)]
    public string? Phone { get; set; }

    public int UserId { get; set; }

    public ApplicationUser User { get; set; } = null!;

    // Navigation property
    public ICollection<BorrowRecord> BorrowRecords { get; set; } = new List<BorrowRecord>();
}
