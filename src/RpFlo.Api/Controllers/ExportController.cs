using System.Text;
using Microsoft.AspNetCore.Mvc;
using RpFlo.Application.Services;

namespace RpFlo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ExportController(ProcurementService service) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv(CancellationToken ct)
    {
        var items = await service.GetAllAsync(ct);

        var sb = new StringBuilder();
        sb.AppendLine("Id,Title,Department,Urgency,Status,TotalAmount,Currency,Requester,CreatedAt,UpdatedAt");

        foreach (var item in items)
        {
            sb.AppendLine(string.Join(",",
                item.Id,
                EscapeCsv(item.Title),
                item.Department,
                item.Urgency,
                item.Status,
                item.TotalAmount,
                item.Currency,
                EscapeCsv(item.RequesterName),
                item.CreatedAt.ToString("O"),
                item.UpdatedAt.ToString("O")));
        }

        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "procurement-requests.csv");
    }

    private static string EscapeCsv(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
}
