using LibraryManagement.Data;
using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = "Admin,Librarian")]
public class DashboardController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly InventoryReconciliationService _reconciliation;

    public DashboardController(ApplicationDbContext db, InventoryReconciliationService reconciliation)
    {
        _db = db;
        _reconciliation = reconciliation;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        var report = await _reconciliation.ReportAsync();
        var today = LoanRules.BusinessDate(DateTime.UtcNow).ToDateTime(TimeOnly.MinValue);
        var overdue = await _db.BorrowRecords.CountAsync(r => r.ReturnDate == null && r.ExpiredDate < today);
        var owned = InventoryReconciliationRules.VerifiedOwnedSum(report.Books);
        return Ok(new DashboardMetricsDto(owned, overdue, report.PendingCount));
    }
}
