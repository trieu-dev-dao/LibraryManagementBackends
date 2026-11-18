using LibraryManagement.Models;

namespace LibraryManagement.Repositories;

public interface IMemberRepository
{
    Task<IEnumerable<Member>> GetAllAsync();

    Task<Member?> GetByIdAsync(int id);

    Task UpdateAsync(Member member);

    Task DeleteAsync(int id);
}