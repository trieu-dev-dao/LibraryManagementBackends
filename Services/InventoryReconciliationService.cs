using System.Data;
using LibraryManagement.Data;
using LibraryManagement.DTOs;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

public class InventoryReconciliationService
{
    private readonly ApplicationDbContext _db;
    public InventoryReconciliationService(ApplicationDbContext db) => _db = db;

    public async Task<InventoryReconciliationReportDto> ReportAsync()
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var books = await _db.Books.AsNoTracking().OrderBy(b => b.Id).ToListAsync();
        var records = await _db.BorrowRecords.AsNoTracking()
            .Select(r => new { r.BookId, r.ReturnDate, r.Status }).ToListAsync();
        var loansByBook = records.GroupBy(r => r.BookId).ToDictionary(g => g.Key, g => g.ToList());
        var rows = books.Select(book =>
        {
            var loans = loansByBook.TryGetValue(book.Id, out var found) ? found : [];
            var open = loans.Count(r => r.ReturnDate is null);
            var mismatch = loans.Count(r =>
                (r.ReturnDate is null && r.Status == LoanRules.Returned) ||
                (r.ReturnDate is not null && r.Status != LoanRules.Returned));
            return InventoryReconciliationRules.Assess(book, open, mismatch);
        }).ToList();
        await tx.CommitAsync();
        return new(rows.Count(r => r.Status == "verified"),
            rows.Count(r => r.Status != "verified"),
            rows.Count(r => r.Status == "inconsistent"), rows);
    }

    public async Task<InventoryReconciliationPreviewDto?> PreviewOrApplyAsync(
        InventoryReconciliationInputDto input, bool apply)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var book = await _db.Books.FirstOrDefaultAsync(b => b.Id == input.BookId);
        if (book is null) return null;
        var loans = await _db.BorrowRecords.AsNoTracking()
            .Where(r => r.BookId == book.Id)
            .Select(r => new { r.ReturnDate, r.Status }).ToListAsync();
        var open = loans.Count(r => r.ReturnDate is null);
        var mismatch = loans.Count(r =>
            (r.ReturnDate is null && r.Status == LoanRules.Returned) ||
            (r.ReturnDate is not null && r.Status != LoanRules.Returned));
        var state = InventoryReconciliationRules.Assess(book, open, mismatch);
        var error = InventoryReconciliationRules.Validate(input, book, open, mismatch);
        if (error is not null)
            return new(book.Id, false, error, book.TotalQuantity,
                input.VerifiedTotalQuantity, state.Status);

        if (apply && !(state.Status == "verified" &&
            book.TotalQuantity == input.VerifiedTotalQuantity &&
            book.TotalQuantitySource == input.Source.Trim()))
        {
            var now = DateTime.UtcNow;
            book.TotalQuantity = input.VerifiedTotalQuantity;
            book.TotalQuantitySource = input.Source.Trim();
            book.TotalQuantityVerifiedAtUtc = now;
            _db.InventoryVerifications.Add(new InventoryVerification
            {
                BookId = book.Id, TotalQuantity = input.VerifiedTotalQuantity,
                AvailableQuantity = input.VerifiedAvailableCopies,
                OutstandingBorrowedCopies = input.VerifiedOutstandingBorrowedCopies,
                Source = input.Source.Trim(), VerifiedAtUtc = now
            });
            await _db.SaveChangesAsync();
        }
        await tx.CommitAsync();
        return new(book.Id, true, apply ? "Verified total accepted." : "Ready to apply.",
            book.TotalQuantity, input.VerifiedTotalQuantity, apply ? "verified" : state.Status);
    }
}
