using LibraryManagement.DTOs;
using LibraryManagement.Models;
using LibraryManagement.Repositories;
using LibraryManagement.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;
namespace LibraryManagement.Services;

public class BookService : IBookService
{
    private readonly IBookRepository _repository;
    private readonly ApplicationDbContext _db;
    public BookService(IBookRepository repository, ApplicationDbContext db)
    {
        _repository = repository;
        _db = db;
    }
    public async Task<IEnumerable<BookResponseDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(Map);
    public async Task<BookResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }
    public async Task<BookResponseDto> CreateAsync(BookCreateDto dto)
    {
        var now = DateTime.UtcNow;
        var entity = new Book
        {
            Title = dto.Title,
            AuthorId = dto.AuthorId,
            CategoryId = dto.CategoryId,
            Quantity = dto.Quantity,
            TotalQuantity = dto.Quantity,
            TotalQuantitySource = "book-create",
            TotalQuantityVerifiedAtUtc = now,
            PublishedYear = dto.PublishedYear
        };

        _db.Books.Add(entity);
        _db.InventoryVerifications.Add(new InventoryVerification
        {
            Book = entity, TotalQuantity = dto.Quantity, AvailableQuantity = dto.Quantity,
            OutstandingBorrowedCopies = 0, Source = "book-create", VerifiedAtUtc = now
        });
        await _db.SaveChangesAsync();

        // Lấy lại sách cùng Author và Category
        var savedBook = await _repository.GetByIdAsync(entity.Id);

        return Map(savedBook!);
    }
    public async Task<BookResponseDto?> UpdateAsync(int id, BookUpdateDto dto)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;
        if (entity.TotalQuantity is int owned &&
            !string.IsNullOrWhiteSpace(entity.TotalQuantitySource) &&
            entity.TotalQuantityVerifiedAtUtc.HasValue)
        {
            var outstanding = await _db.BorrowRecords.CountAsync(r => r.BookId == id && r.ReturnDate == null);
            var nextOwned = InventoryRules.OwnedAfterAvailableAdjustment(
                owned, entity.Quantity, dto.Quantity, outstanding);
            if (nextOwned != owned)
            {
                var now = DateTime.UtcNow;
                entity.TotalQuantity = nextOwned;
                entity.TotalQuantitySource = "book-update:available-adjustment";
                entity.TotalQuantityVerifiedAtUtc = now;
                _db.InventoryVerifications.Add(new InventoryVerification
                {
                    BookId = id, TotalQuantity = nextOwned,
                    AvailableQuantity = dto.Quantity, OutstandingBorrowedCopies = outstanding,
                    Source = entity.TotalQuantitySource, VerifiedAtUtc = now
                });
            }
        }
        entity.Title = dto.Title; entity.AuthorId = dto.AuthorId; entity.CategoryId = dto.CategoryId;
        entity.Quantity = dto.Quantity; entity.PublishedYear = dto.PublishedYear;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();
        return Map(entity);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null) return false;
        await _repository.DeleteAsync(id); return true;
    }
    private static BookResponseDto Map(Book e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        AuthorId = e.AuthorId,
        AuthorName = e.Author?.Name ?? string.Empty,
        CategoryId = e.CategoryId,
        CategoryName = e.Category?.Name ?? string.Empty,
        Quantity = e.Quantity,
        TotalQuantity = e.TotalQuantity,
        PublishedYear = e.PublishedYear
    };
}
