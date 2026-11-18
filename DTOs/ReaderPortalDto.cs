using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public record ReaderLoanDto(
    int Id, string BookTitle, int Quantity, DateTime BorrowDate, DateOnly DueDate,
    DateTime? ReturnDate, string Status, bool IsCompleted);

public record ReaderLoansDto(int ActiveLoanCount, IReadOnlyList<ReaderLoanDto> Loans);

public record ReaderProfileDto(
    int UserId, int? MemberId, string Username, string Email, string FullName, string? Phone);

public class ReaderProfileUpdateDto
{
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Phone { get; set; }
}
