using LibraryManagement.DTOs;
using LibraryManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryManagement.Controllers;

[ApiController]
[Route("api/inventory/reconciliation")]
[Authorize(Roles = "Admin,Librarian")]
public class InventoryReconciliationController : ControllerBase
{
    private readonly InventoryReconciliationService _service;
    public InventoryReconciliationController(InventoryReconciliationService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<InventoryReconciliationReportDto>> Report() =>
        Ok(await _service.ReportAsync());

    [HttpPost("preview")]
    public async Task<ActionResult<InventoryReconciliationPreviewDto>> Preview(InventoryReconciliationInputDto input)
    {
        var result = await _service.PreviewOrApplyAsync(input, apply: false);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("apply")]
    public async Task<ActionResult<InventoryReconciliationPreviewDto>> Apply(InventoryReconciliationInputDto input)
    {
        var result = await _service.PreviewOrApplyAsync(input, apply: true);
        return result is null ? NotFound() : result.CanApply ? Ok(result) : Conflict(result);
    }
}
