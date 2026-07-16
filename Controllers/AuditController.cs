using Microsoft.AspNetCore.Mvc;
using Week6.Data;

namespace Week6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuditController : ControllerBase
{
    private readonly IAuditRepository _audit;
    public AuditController(IAuditRepository audit) => _audit = audit;

    [HttpGet("report")]
    public async Task<IActionResult> GetReport(
        [FromQuery] DateTime startDate, [FromQuery] DateTime endDate,
        [FromQuery] string? tableName, CancellationToken ct)
    {
        var rows = await _audit.GetAuditReportAsync(startDate, endDate, tableName, ct);
        return Ok(rows);
    }
}