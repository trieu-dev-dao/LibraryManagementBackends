using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _service;
    public CategoriesController(ICategoryService service) => _service = service;

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<CategoryResponseDto>>> GetAll() => Ok(await _service.GetAllAsync());

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryResponseDto>> GetById(int id) { var x = await _service.GetByIdAsync(id); return x is null ? NotFound() : Ok(x); }

    [HttpPost]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<CategoryResponseDto>> Create(CategoryCreateDto dto) => Ok(await _service.CreateAsync(dto));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<CategoryResponseDto>> Update(int id, CategoryUpdateDto dto) { var x = await _service.UpdateAsync(id, dto); return x is null ? NotFound() : Ok(x); }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Delete(int id) => await _service.DeleteAsync(id) ? NoContent() : NotFound();
}