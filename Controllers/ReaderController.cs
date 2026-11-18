using LibraryManagement.Data;
using LibraryManagement.DTOs;
using LibraryManagement.Models;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/reader")]
[Authorize(Roles = "User")]
public class ReaderController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    public ReaderController(ApplicationDbContext db) => _db = db;

    [HttpGet("loans")]
    public async Task<ActionResult<ReaderLoansDto>> Loans()
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var account = await _db.Users.AsNoTracking().Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.Id == userId);
        if (account is null) return Unauthorized();
        if (account.Member is null)
            return Conflict(new { code = "member_not_linked", message = "Tài khoản chưa có hồ sơ độc giả." });

        var records = await _db.BorrowRecords.AsNoTracking().Include(r => r.Book)
            .Where(r => r.MemberId == account.Member.Id)
            .OrderByDescending(r => r.BorrowDate).ThenByDescending(r => r.Id).ToListAsync();
        return Ok(new ReaderLoansDto(records.Count(r => r.ReturnDate is null),
            records.Select(r => new ReaderLoanDto(
                r.Id, r.Book?.Title ?? string.Empty, 1,
                DateTime.SpecifyKind(r.BorrowDate, DateTimeKind.Utc),
                DateOnly.FromDateTime(r.ExpiredDate),
                r.ReturnDate.HasValue ? DateTime.SpecifyKind(r.ReturnDate.Value, DateTimeKind.Utc) : null,
                LoanRules.Status(r), r.ReturnDate.HasValue)).ToList()));
    }

    [HttpGet("profile")]
    public async Task<ActionResult<ReaderProfileDto>> Profile()
    {
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var account = await _db.Users.AsNoTracking().Include(u => u.Member)
            .FirstOrDefaultAsync(u => u.Id == userId);
        return account is null ? Unauthorized() : Ok(MapProfile(account));
    }

    [HttpPut("profile")]
    public async Task<ActionResult<ReaderProfileDto>> UpdateProfile(ReaderProfileUpdateDto dto)
    {
        var fullName = dto.FullName.Trim();
        if (fullName.Length == 0)
        {
            ModelState.AddModelError(nameof(dto.FullName), "Họ tên không được để trống.");
            return ValidationProblem(ModelState);
        }
        var userId = CurrentUserId();
        if (userId is null) return Unauthorized();
        var account = await _db.Users.Include(u => u.Member).FirstOrDefaultAsync(u => u.Id == userId);
        if (account is null) return Unauthorized();

        account.FullName = fullName;
        account.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
        if (account.Member is not null)
        {
            account.Member.FullName = account.FullName;
            account.Member.Phone = account.Phone;
        }
        await _db.SaveChangesAsync();
        return Ok(MapProfile(account));
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirst("userId")?.Value, out var id) && id > 0 ? id : null;

    private static ReaderProfileDto MapProfile(ApplicationUser account) => new(
        account.Id, account.Member?.Id, account.UserName ?? string.Empty,
        account.Email ?? string.Empty, account.FullName, account.Phone);
}
