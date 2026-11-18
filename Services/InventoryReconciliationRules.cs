using LibraryManagement.DTOs;
using LibraryManagement.Models;

namespace LibraryManagement.Services;

public static class InventoryReconciliationRules
{
    public static long? VerifiedOwnedSum(IEnumerable<InventoryReconciliationBookDto> rows)
    {
        long sum = 0;
        foreach (var row in rows)
        {
            if (row.Status != "verified" || row.TotalQuantity is null) return null;
            sum = checked(sum + row.TotalQuantity.Value);
        }
        return sum;
    }

    public static InventoryReconciliationBookDto Assess(Book book, int observedOpen, int statusMismatches)
    {
        var hasProvenance = !string.IsNullOrWhiteSpace(book.TotalQuantitySource) &&
            book.TotalQuantityVerifiedAtUtc.HasValue;
        var expected = (long)book.Quantity + observedOpen;
        var inconsistent = book.Quantity < 0 || statusMismatches > 0 ||
            (book.TotalQuantity.HasValue && book.TotalQuantity.Value != expected) ||
            ((book.TotalQuantitySource is not null || book.TotalQuantityVerifiedAtUtc.HasValue) &&
             (!hasProvenance || !book.TotalQuantity.HasValue));
        var status = inconsistent ? "inconsistent" :
            hasProvenance && book.TotalQuantity.HasValue ? "verified" : "pending";
        var reason = status switch
        {
            "verified" => "Verified total and loan ledger match the available stock.",
            "inconsistent" when statusMismatches > 0 => "Stored loan status conflicts with return date; review the loan ledger.",
            "inconsistent" => "Total, available stock, and open loan copies do not match, or provenance is incomplete.",
            _ => "No verified source; staff must confirm the physical total and outstanding copies."
        };
        return new(book.Id, book.Title, book.Quantity, observedOpen,
            status == "verified" ? observedOpen : null, book.TotalQuantity,
            book.TotalQuantitySource, status, reason);
    }

    public static string? Validate(InventoryReconciliationInputDto input, Book book,
        int observedOpen, int statusMismatches)
    {
        if (string.IsNullOrWhiteSpace(input.Source) || input.Source.Trim().Length < 3)
            return "Enter a specific verification source (for example, physical count and loan ledger).";
        if (book.Quantity < 0 || statusMismatches > 0)
            return "Stock or stored loan statuses are inconsistent; correct the ledger before verification.";
        if (input.VerifiedAvailableCopies != book.Quantity)
            return $"Available stock changed: database has {book.Quantity}; preview again after checking the shelves.";
        if (input.VerifiedOutstandingBorrowedCopies != observedOpen)
            return $"Outstanding copies differ: database has {observedOpen} unreturned one-copy loans; reconcile the ledger first.";
        if ((long)input.VerifiedAvailableCopies + input.VerifiedOutstandingBorrowedCopies != input.VerifiedTotalQuantity)
            return "Owned total must equal verified available copies plus verified outstanding copies; reconcile missing or extra copies first.";
        return null;
    }
}
