using LibraryManagement.DTOs;
using LibraryManagement.Models;
using LibraryManagement.Repositories;
namespace LibraryManagement.Services;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _repository;
    public MemberService(IMemberRepository repository) => _repository = repository;
    public async Task<IEnumerable<MemberResponseDto>> GetAllAsync() => (await _repository.GetAllAsync()).Select(Map);
    public async Task<MemberResponseDto?> GetByIdAsync(int id)
    {
        var entity = await _repository.GetByIdAsync(id);
        return entity is null ? null : Map(entity);
    }
    public async Task<MemberResponseDto?> UpdateAsync(int id, MemberUpdateDto dto)
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity is null) return null;
        entity.FullName = dto.FullName; entity.Email = dto.Email; entity.Phone = dto.Phone;
        await _repository.UpdateAsync(entity);
        return Map(entity);
    }
    public async Task<bool> DeleteAsync(int id)
    {
        if (await _repository.GetByIdAsync(id) is null) return false;
        await _repository.DeleteAsync(id); return true;
    }
    private static MemberResponseDto Map(Member e) => new() { Id = e.Id, FullName = e.FullName, Email = e.Email, Phone = e.Phone };
}