using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/members")]
[Authorize]
public class MembersController : ControllerBase
{
    private readonly IMemberService _service;
    public MembersController(IMemberService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<IEnumerable<MemberResponseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<MemberResponseDto>> GetById(int id) { var x = await _service.GetByIdAsync(id); return x is null ? NotFound() : Ok(x); }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<MemberResponseDto>> Update(int id, MemberUpdateDto dto) { var x = await _service.UpdateAsync(id, dto); return x is null ? NotFound() : Ok(x); }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Delete(int id) => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}