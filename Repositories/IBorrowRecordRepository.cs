using LibraryManagement.Models;

namespace LibraryManagement.Repositories;

public interface IBorrowRecordRepository
{
    Task<IEnumerable<BorrowRecord>> GetAllAsync();

    Task<BorrowRecord?> GetByIdAsync(int id);

    Task AddAsync(BorrowRecord borrowRecord);

    Task UpdateAsync(BorrowRecord borrowRecord);

    Task DeleteAsync(int id);
}