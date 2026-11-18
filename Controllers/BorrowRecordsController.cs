using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/borrow-records")]
[Authorize]
public class BorrowRecordsController : ControllerBase
{
    private readonly IBorrowService _service;
    public BorrowRecordsController(IBorrowService service) => _service = service;

    [HttpGet]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<IEnumerable<BorrowRecordResponseDto>>> GetAll([FromQuery] string? status)
    {
        if (status is not null && status != LoanRules.Borrowing && status != LoanRules.Overdue && status != LoanRules.Returned)
            return BadRequest(new { message = "Trạng thái phiếu mượn không hợp lệ." });
        return Ok(await _service.GetAllAsync(status));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<ActionResult<BorrowRecordResponseDto>> GetById(int id) { var x = await _service.GetByIdAsync(id); return x is null ? NotFound() : Ok(x); }

    [HttpPost("borrow")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Borrow(BorrowRecordCreateDto dto) { try { return Ok(await _service.BorrowAsync(dto)); } catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); } }

    [HttpPost("{id:int}/return")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Return(int id) { try { var x = await _service.ReturnAsync(id); return x is null ? NotFound() : Ok(x); } catch (InvalidOperationException e) { return BadRequest(new { message = e.Message }); } }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Update(int id, BorrowRecordUpdateDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(id, dto);
            return result is null
                ? NotFound(new { message = "Không tìm thấy phiếu mượn." })
                : Ok(result);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin, Librarian")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _service.DeleteAsync(id);

            return result
                ? Ok(new { message = "Xóa phiếu mượn thành công." })
                : NotFound(new { message = "Không tìm thấy phiếu mượn." });
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(new { message = e.Message });
        }
    }
}
