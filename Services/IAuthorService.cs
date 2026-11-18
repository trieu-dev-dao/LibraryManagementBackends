using LibraryManagement.DTOs;
namespace LibraryManagement.Services;

public interface IAuthorService
{
    Task<IEnumerable<AuthorResponseDto>> GetAllAsync();
    Task<AuthorResponseDto?> GetByIdAsync(int id);
    Task<AuthorResponseDto> CreateAsync(AuthorCreateDto dto);
    Task<AuthorResponseDto?> UpdateAsync(int id, AuthorUpdateDto dto);
    Task<bool> DeleteAsync(int id);
}