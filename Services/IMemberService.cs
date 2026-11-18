using LibraryManagement.DTOs;
namespace LibraryManagement.Services;

public interface IMemberService
{
    Task<IEnumerable<MemberResponseDto>> GetAllAsync();
    Task<MemberResponseDto?> GetByIdAsync(int id);
    Task<MemberResponseDto?> UpdateAsync(int id, MemberUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}