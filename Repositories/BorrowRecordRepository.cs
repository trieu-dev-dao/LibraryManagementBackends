using LibraryManagement.Data;
using LibraryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Repositories;

public class BorrowRecordRepository : IBorrowRecordRepository
{
    private readonly ApplicationDbContext _context;

    public BorrowRecordRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<BorrowRecord>> GetAllAsync()
    {
        return await _context.BorrowRecords
            .Include(br => br.Book)
            .Include(br => br.Member)
            .ToListAsync();
    }

    public async Task<BorrowRecord?> GetByIdAsync(int id)
    {
        return await _context.BorrowRecords
            .Include(br => br.Book)
            .Include(br => br.Member)
            .FirstOrDefaultAsync(br => br.Id == id);
    }

    public async Task AddAsync(BorrowRecord borrowRecord)
    {
        await _context.BorrowRecords.AddAsync(borrowRecord);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(BorrowRecord borrowRecord)
    {
        _context.BorrowRecords.Update(borrowRecord);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var record = await _context.BorrowRecords.FindAsync(id);

        if (record is not null)
        {
            _context.BorrowRecords.Remove(record);
            await _context.SaveChangesAsync();
        }
    }
}