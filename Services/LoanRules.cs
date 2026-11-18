using LibraryManagement.Models;

namespace LibraryManagement.Services;

public static class LoanRules
{
    public const string Borrowing = "Đang mượn";
    public const string Overdue = "Quá hạn";
    public const string Returned = "Đã trả";

    // Vietnam has used UTC+07:00 without daylight saving time for these records.
    private static readonly TimeSpan BusinessOffset = TimeSpan.FromHours(7);

    public static DateOnly BusinessDate(DateTime utc) =>
        DateOnly.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc).Add(BusinessOffset));

    public static bool IsCurrentlyOverdue(BorrowRecord record, DateOnly today) =>
        record.ReturnDate is null && DateOnly.FromDateTime(record.ExpiredDate) < today;

    // Display status. ReturnDate, not the display value, determines whether the loan is open.
    public static string Status(BorrowRecord record) =>
        record.ReturnDate is null ? Borrowing : IsLateReturn(record) ? Overdue : Returned;

    // Stored status continues to describe the loan lifecycle for existing inventory reads.
    public static string StoredStatus(BorrowRecord record) =>
        record.ReturnDate is null ? Borrowing : Returned;

    public static void EnsureReturnable(BorrowRecord record)
    {
        if (record.ReturnDate.HasValue)
            throw new InvalidOperationException("Phiếu mượn này đã được trả trước đó.");
    }

    public static bool IsLateReturn(BorrowRecord record) =>
        record.ReturnDate.HasValue &&
        BusinessDate(record.ReturnDate.Value) > DateOnly.FromDateTime(record.ExpiredDate);
}
