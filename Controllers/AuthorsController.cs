using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/authors")]
[Authorize]
public class AuthorsController : ControllerBase
{
    private readonly IAuthorService _service;
    public AuthorsController(IAuthorService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<AuthorResponseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthorResponseDto>> GetById(int id) { var x = await _service.GetByIdAsync(id); return x is null ? NotFound() : Ok(x); }

    [HttpPost]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<AuthorResponseDto>> Create(AuthorCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<AuthorResponseDto>> Update(int id, AuthorUpdateDto dto) { var x = await _service.UpdateAsync(id, dto); return x is null ? NotFound() : Ok(x); }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Delete(int id) => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}