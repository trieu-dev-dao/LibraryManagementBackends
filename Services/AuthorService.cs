using LibraryManagement.DTOs;
using LibraryManagement.Models;
using LibraryManagement.Repositories;
namespace LibraryManagement.Services;

public class AuthorService : IAuthorService
{
    private readonly IAuthorRepository _repository;
    public AuthorService(IAuthorRepository repository) => _repository = repository;
    public async Task<IEnumerable<AuthorResponseDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(Map);
    public async Task<AuthorResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }
    public async Task<AuthorResponseDto> CreateAsync(AuthorCreateDto dto)
    {
        var entity = new Author { Name = dto.Name, Bio = dto.Bio };
        await _repository.AddAsync(entity);
        return Map(entity);
    }
    public async Task<AuthorResponseDto?> UpdateAsync(int id, AuthorUpdateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;
        entity.Name = dto.Name; entity.Bio = dto.Bio;
        await _repository.UpdateAsync(entity);
        return Map(entity);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null) return false;
        await _repository.DeleteAsync(id); return true;
    }
    private static AuthorResponseDto Map(Author e) => new() { Id = e.Id, Name = e.Name, Bio = e.Bio };
}