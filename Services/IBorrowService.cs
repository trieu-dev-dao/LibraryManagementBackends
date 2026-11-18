using LibraryManagement.DTOs;
namespace LibraryManagement.Services;

public interface IBorrowService
{
    Task<IEnumerable<BorrowRecordResponseDto>> GetAllAsync(string? status = null);
    Task<BorrowRecordResponseDto?> GetByIdAsync(int id);
    Task<BorrowRecordResponseDto> BorrowAsync(BorrowRecordCreateDto dto);
    Task<BorrowRecordResponseDto?> ReturnAsync(int id);
    Task<BorrowRecordResponseDto?> UpdateAsync(int id, BorrowRecordUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}
