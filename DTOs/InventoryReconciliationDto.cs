using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.DTOs;

public record InventoryReconciliationBookDto(
    int BookId, string Title, int AvailableCopies, int ObservedOutstandingLoanRecords,
    int? VerifiedOutstandingBorrowedCopies, int? TotalQuantity, string? TotalQuantitySource,
    string Status, string Reason);

public record InventoryReconciliationReportDto(
    int VerifiedCount, int PendingCount, int InconsistentCount,
    IReadOnlyList<InventoryReconciliationBookDto> Books);

public class InventoryReconciliationInputDto
{
    [Range(1, int.MaxValue)] public required int BookId { get; set; }
    [Range(0, int.MaxValue)] public required int VerifiedAvailableCopies { get; set; }
    [Range(0, int.MaxValue)] public required int VerifiedOutstandingBorrowedCopies { get; set; }
    [Range(0, int.MaxValue)] public required int VerifiedTotalQuantity { get; set; }
    [Required, StringLength(200, MinimumLength = 3)] public required string Source { get; set; }
}

public record InventoryReconciliationPreviewDto(
    int BookId, bool CanApply, string Reason, int? CurrentTotalQuantity,
    int VerifiedTotalQuantity, string CurrentStatus);
