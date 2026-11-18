using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models;

public class BorrowRecord
{
    public int Id { get; set; }

    public int BookId { get; set; }

    public int MemberId { get; set; }

    public DateTime BorrowDate { get; set; } = DateTime.UtcNow;

    public DateTime ExpiredDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    [Required]
    [StringLength(20)]
    public string Status { get; set; } = "Đang mượn";

    // Navigation properties
    [ForeignKey(nameof(BookId))]
    public Book? Book { get; set; }

    [ForeignKey(nameof(MemberId))]
    public Member? Member { get; set; }
}
