using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController : ControllerBase
{
    private readonly IBookService _service;
    public BooksController(IBookService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<BookResponseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<BookResponseDto>> GetById(int id) { var x = await _service.GetByIdAsync(id); return x is null ? NotFound() : Ok(x); }

    [HttpPost]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<BookResponseDto>> Create(BookCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<BookResponseDto>> Update(int id, BookUpdateDto dto)
    {
        try { var x = await _service.UpdateAsync(id, dto); return x is null ? NotFound() : Ok(x); }
        catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Delete(int id) => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}
