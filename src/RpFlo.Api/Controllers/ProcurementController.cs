using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using RpFlo.Api.Documents;
using RpFlo.Application.DTOs;
using RpFlo.Application.Services;
using RpFlo.Domain.Common;

namespace RpFlo.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProcurementController(
    ProcurementService service,
    IValidator<CreateProcurementRequest> createValidator,
    IValidator<UpdateProcurementRequest> updateValidator,
    IValidator<AddLineItemsRequest> addLineItemsValidator,
    IValidator<RejectionRequest> rejectionValidator,
    IValidator<AddCommentRequest> commentValidator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProcurementListItem>>> GetAll(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] ProcurementListPageQuery query,
        CancellationToken ct) =>
        ToActionResult(await service.GetPagedVisibleForUserAsync(userId, query, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProcurementResponse>> GetById(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.GetByIdForUserAsync(id, userId, ct));

    [HttpGet("my")]
    public async Task<ActionResult<PagedResult<ProcurementListItem>>> GetMy(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] ProcurementListPageQuery query,
        CancellationToken ct) =>
        ToActionResult(await service.GetPagedByRequesterAsync(userId, query, ct));

    [HttpGet("pending")]
    public async Task<ActionResult<PagedResult<ProcurementListItem>>> GetPending(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromQuery] ProcurementTaskPageQuery query,
        CancellationToken ct) =>
        ToActionResult(await service.GetPagedPendingForUserAsync(userId, query, ct));

    [HttpPost]
    public async Task<ActionResult<ProcurementResponse>> Create(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] CreateProcurementRequest request,
        CancellationToken ct)
    {
        await createValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.CreateAsync(request, userId, ct), StatusCodes.Status201Created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProcurementResponse>> Update(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] UpdateProcurementRequest request,
        CancellationToken ct)
    {
        await updateValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.UpdateAsync(id, request, userId, ct));
    }

    [HttpPost("{id:guid}/line-items")]
    public async Task<ActionResult<ProcurementResponse>> AddLineItems(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] AddLineItemsRequest request,
        CancellationToken ct)
    {
        await addLineItemsValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.AddLineItemsAsync(id, request, userId, ct));
    }

    [HttpDelete("{id:guid}/line-items/{lineItemId:guid}")]
    public async Task<ActionResult<ProcurementResponse>> RemoveLineItem(
        Guid id, Guid lineItemId,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.RemoveLineItemAsync(id, lineItemId, userId, ct));

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ProcurementResponse>> Submit(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.SubmitAsync(id, userId, ct));

    [HttpPost("{id:guid}/approve/manager")]
    public async Task<ActionResult<ProcurementResponse>> ApproveByManager(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] ApprovalRequest request,
        CancellationToken ct) =>
        ToActionResult(await service.ApproveByManagerAsync(id, userId, request, ct));

    [HttpPost("{id:guid}/reject/manager")]
    public async Task<ActionResult<ProcurementResponse>> RejectByManager(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] RejectionRequest request,
        CancellationToken ct)
    {
        await rejectionValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.RejectByManagerAsync(id, userId, request, ct));
    }

    [HttpPost("{id:guid}/approve/finance")]
    public async Task<ActionResult<ProcurementResponse>> ApproveByFinance(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] ApprovalRequest request,
        CancellationToken ct) =>
        ToActionResult(await service.ApproveByFinanceAsync(id, userId, request, ct));

    [HttpPost("{id:guid}/reject/finance")]
    public async Task<ActionResult<ProcurementResponse>> RejectByFinance(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] RejectionRequest request,
        CancellationToken ct)
    {
        await rejectionValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.RejectByFinanceAsync(id, userId, request, ct));
    }

    [HttpPost("{id:guid}/issue-po")]
    public async Task<ActionResult<ProcurementResponse>> IssuePurchaseOrder(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.IssuePurchaseOrderAsync(id, userId, ct));

    [HttpPost("{id:guid}/revise")]
    public async Task<ActionResult<ProcurementResponse>> ReviseToDraft(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.ReviseToDraftAsync(id, userId, ct));

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<CommentResponse>> AddComment(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        await commentValidator.ValidateAndThrowAsync(request, ct);
        return ToActionResult(await service.AddCommentAsync(id, userId, request, ct), StatusCodes.Status201Created);
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetrics>> GetMetrics(
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct) =>
        ToActionResult(await service.GetMetricsForUserAsync(userId, ct));

    [HttpGet("{id:guid}/export/pdf")]
    public async Task<IActionResult> ExportPdf(
        Guid id,
        [FromHeader(Name = "X-User-Id")] Guid userId,
        CancellationToken ct)
    {
        var result = await service.GetByIdForUserAsync(id, userId, ct);
        return result.Match<IActionResult>(
            procurement =>
            {
                if (procurement.Status != "PurchaseOrderIssued")
                    return BadRequest(new { Code = "InvalidExport", Message = "PDF export is only available for issued purchase orders." });

                var document = new PurchaseOrderDocument(procurement);
                var pdf = document.GeneratePdf();
                return File(pdf, "application/pdf");
            },
            error => error.Code switch
            {
                var c when c.StartsWith("NotFound") => NotFound(new { error.Code, error.Message }),
                var c when c.StartsWith("Unauthorized") => StatusCode(403, new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            });
    }

    private ActionResult<T> ToActionResult<T>(Result<T> result, int successCode = StatusCodes.Status200OK) =>
        result.Match<ActionResult<T>>(
            value => successCode == StatusCodes.Status201Created
                ? StatusCode(StatusCodes.Status201Created, value)
                : Ok(value),
            error => error.Code switch
            {
                var c when c.StartsWith("NotFound") => NotFound(new { error.Code, error.Message }),
                var c when c.StartsWith("Unauthorized") => StatusCode(403, new { error.Code, error.Message }),
                var c when c.StartsWith("Validation") => BadRequest(new { error.Code, error.Message }),
                _ => BadRequest(new { error.Code, error.Message })
            });
}
