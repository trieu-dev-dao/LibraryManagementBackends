using System.Data;
using LibraryManagement.Data;
using LibraryManagement.DTOs;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Services;

public class BorrowService : IBorrowService
{
    private readonly ApplicationDbContext _db;

    public BorrowService(ApplicationDbContext db) => _db = db;

    public async Task<IEnumerable<BorrowRecordResponseDto>> GetAllAsync(string? status = null)
    {
        var records = await _db.BorrowRecords.AsNoTracking()
            .Include(r => r.Book).Include(r => r.Member).ToListAsync();
        return records.Where(r => status is null || LoanRules.Status(r) == status)
            .Select(Map).ToList();
    }

    public async Task<BorrowRecordResponseDto?> GetByIdAsync(int id)
    {
        var record = await _db.BorrowRecords.AsNoTracking()
            .Include(r => r.Book).Include(r => r.Member)
            .FirstOrDefaultAsync(r => r.Id == id);
        return record is null ? null : Map(record);
    }

    public async Task<BorrowRecordResponseDto> BorrowAsync(BorrowRecordCreateDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var book = await _db.Books.FindAsync(dto.BookId)
            ?? throw new InvalidOperationException("Không tìm thấy sách.");
        var member = await _db.Members.FindAsync(dto.MemberId)
            ?? throw new InvalidOperationException("Không tìm thấy độc giả.");
        var now = DateTime.UtcNow;
        if (DateOnly.FromDateTime(dto.ExpiredDate) < LoanRules.BusinessDate(now))
            throw new InvalidOperationException("Hạn trả không được ở trong quá khứ.");

        InventoryRules.Lend(book);
        var record = new BorrowRecord
        {
            BookId = book.Id, MemberId = member.Id, BorrowDate = now,
            ExpiredDate = dto.ExpiredDate.Date, ReturnDate = null,
            Status = LoanRules.Borrowing, Book = book, Member = member
        };
        _db.BorrowRecords.Add(record);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Map(record);
    }

    public async Task<BorrowRecordResponseDto?> ReturnAsync(int id)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var record = await Record(id);
        if (record is null) return null;
        LoanRules.EnsureReturnable(record);
        var book = record.Book ?? throw new InvalidOperationException("Không tìm thấy sách của phiếu mượn.");
        InventoryRules.Return(book);
        record.ReturnDate = DateTime.UtcNow;
        record.Status = LoanRules.StoredStatus(record);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Map(record);
    }

    public async Task<BorrowRecordResponseDto?> UpdateAsync(int id, BorrowRecordUpdateDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var record = await Record(id);
        if (record is null) return null;
        var book = await _db.Books.FindAsync(dto.BookId)
            ?? throw new InvalidOperationException("Không tìm thấy sách.");
        var member = await _db.Members.FindAsync(dto.MemberId)
            ?? throw new InvalidOperationException("Không tìm thấy độc giả.");
        if (DateOnly.FromDateTime(dto.ExpiredDate) < LoanRules.BusinessDate(record.BorrowDate))
            throw new InvalidOperationException("Hạn trả không được trước ngày mượn.");

        // Status is derived. The old input field is accepted for compatibility but cannot
        // mark a loan returned; the return endpoint is the only return transition.
        if (record.ReturnDate is null && record.BookId != book.Id)
        {
            InventoryRules.Lend(book);
            InventoryRules.Return(record.Book!);
        }
        record.BookId = book.Id;
        record.MemberId = member.Id;
        record.ExpiredDate = dto.ExpiredDate.Date;
        record.Book = book;
        record.Member = member;
        record.Status = LoanRules.StoredStatus(record);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Map(record);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var record = await Record(id);
        if (record is null) return false;
        if (record.ReturnDate is null)
            InventoryRules.Return(record.Book!);
        _db.BorrowRecords.Remove(record);
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return true;
    }

    private Task<BorrowRecord?> Record(int id) => _db.BorrowRecords
        .Include(r => r.Book).Include(r => r.Member)
        .FirstOrDefaultAsync(r => r.Id == id);

    private static BorrowRecordResponseDto Map(BorrowRecord r) => new()
    {
        Id = r.Id, BookId = r.BookId, BookTitle = r.Book?.Title,
        MemberId = r.MemberId, MemberFullName = r.Member?.FullName,
        BorrowDate = DateTime.SpecifyKind(r.BorrowDate, DateTimeKind.Utc),
        ExpiredDate = r.ExpiredDate,
        ReturnDate = r.ReturnDate.HasValue
            ? DateTime.SpecifyKind(r.ReturnDate.Value, DateTimeKind.Utc) : null,
        Status = LoanRules.Status(r),
        IsLateReturn = LoanRules.IsLateReturn(r)
    };
}
