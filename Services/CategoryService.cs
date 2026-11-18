using LibraryManagement.DTOs;
using LibraryManagement.Models;
using LibraryManagement.Repositories;
namespace LibraryManagement.Services;

public class CategoryService : ICategoryService
{
    private readonly ICategoryRepository _repository;
    public CategoryService(ICategoryRepository repository) => _repository = repository;
    public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(Map);
    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }
    public async Task<CategoryResponseDto> CreateAsync(CategoryCreateDto dto)
    {
        var entity = new Category { Name = dto.Name };
        await _repository.AddAsync(entity);
        return Map(entity);
    }
    public async Task<CategoryResponseDto?> UpdateAsync(int id, CategoryUpdateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;
        entity.Name = dto.Name;
        await _repository.UpdateAsync(entity);
        return Map(entity);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null) return false;
        await _repository.DeleteAsync(id); return true;
    }
    private static CategoryResponseDto Map(Category e) => new() { Id = e.Id, Name = e.Name };
}