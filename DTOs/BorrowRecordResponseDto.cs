namespace LibraryManagement.DTOs;

public class BorrowRecordResponseDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string? BookTitle { get; set; }
    public int MemberId { get; set; }
    public string? MemberFullName { get; set; }
    public DateTime BorrowDate { get; set; }
    public DateTime ExpiredDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsLateReturn { get; set; }
}
