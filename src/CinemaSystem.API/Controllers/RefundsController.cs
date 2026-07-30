using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common.DTOs.Refunds;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Refunds;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api")]
public sealed class RefundsController(IRefundService refundService, IRefundAuditService auditService) : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("refund-policy")]
    public ActionResult<ApiResponse<RefundPolicyDto>> GetPolicy() =>
        Ok(ApiResponse<RefundPolicyDto>.Success(refundService.GetPolicy(), "Refund policy retrieved successfully."));

    [Authorize(Roles = nameof(UserRole.Customer))]
    [EnableRateLimiting("refund-ip")]
    [HttpPost("refunds")]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> Create(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateRefundRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            return BadRequest(ApiResponse<RefundResponseDto>.Fail("Idempotency-Key must be a UUID."));
        }

        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<RefundResponseDto>.Fail("Unauthorized access."));
        }

        var result = await refundService.CreateAsync(customerId.Value, idempotencyKey!, request, cancellationToken);
        await AuditAsync("REFUND_CREATE", result.RefundId, cancellationToken);
        return Ok(ApiResponse<RefundResponseDto>.Success(result, "Refund credited to the customer wallet."));
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("refunds/me")]
    public async Task<ActionResult<ApiResponse<RefundPagedResultDto>>> GetMine(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<RefundPagedResultDto>.Fail("Unauthorized access."));
        }

        var result = await refundService.GetMineAsync(customerId.Value, new RefundListQueryRequest
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        await AuditAsync("REFUND_VIEW_OWN", null, cancellationToken);
        return Ok(ApiResponse<RefundPagedResultDto>.Success(result, "Refund requests retrieved successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpGet("ops/refunds")]
    public async Task<ActionResult<ApiResponse<RefundPagedResultDto>>> GetForOperations(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await refundService.GetForOperationsAsync(new RefundListQueryRequest
        {
            Status = status,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);
        await AuditAsync("REFUND_VIEW_OPS", null, cancellationToken);
        return Ok(ApiResponse<RefundPagedResultDto>.Success(result, "Operational refund requests retrieved successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpPost("ops/refunds/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> Approve(
        Guid id,
        [FromBody] RefundDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        if (actorId is null) return Unauthorized(ApiResponse<RefundResponseDto>.Fail("Unauthorized access."));
        var result = await refundService.ApproveAsync(id, actorId.Value, request.InternalNote, cancellationToken);
        await AuditAsync("REFUND_APPROVE", result.RefundId, cancellationToken);
        return Ok(ApiResponse<RefundResponseDto>.Success(result, "Refund approved and credited to the customer wallet."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpPost("ops/refunds/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<RefundResponseDto>>> Reject(
        Guid id,
        [FromBody] RefundDecisionRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = GetCurrentUserId();
        if (actorId is null) return Unauthorized(ApiResponse<RefundResponseDto>.Fail("Unauthorized access."));
        var result = await refundService.RejectAsync(id, actorId.Value, request, cancellationToken);
        await AuditAsync("REFUND_REJECT", result.RefundId, cancellationToken);
        return Ok(ApiResponse<RefundResponseDto>.Success(result, "Refund request rejected."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private Task AuditAsync(string action, Guid? refundId, CancellationToken cancellationToken) =>
        auditService.LogAsync(
            GetCurrentUserId(),
            action,
            refundId,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Path,
            cancellationToken);
}
