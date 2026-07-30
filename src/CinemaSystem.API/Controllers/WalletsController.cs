using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.DTOs.Wallets;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api")]
public sealed class WalletsController(IWalletService walletService, IWalletTopUpService walletTopUpService) : ControllerBase
{
    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("wallet")]
    public async Task<ActionResult<ApiResponse<WalletSummaryDto>>> GetMine(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null) return Unauthorized(ApiResponse<WalletSummaryDto>.Fail("Unauthorized access."));
        var result = await walletService.GetMineAsync(customerId.Value, page, pageSize, cancellationToken);
        return Ok(ApiResponse<WalletSummaryDto>.Success(result, "Wallet retrieved successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpPost("wallet/topups")]
    public async Task<ActionResult<ApiResponse<WalletTopUpResponseDto>>> CreateTopUp(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateWalletTopUpRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
            return BadRequest(ApiResponse<WalletTopUpResponseDto>.Fail("Idempotency-Key must be a UUID."));
        var customerId = GetCurrentUserId();
        if (customerId is null) return Unauthorized(ApiResponse<WalletTopUpResponseDto>.Fail("Unauthorized access."));
        var result = await walletTopUpService.CreateAsync(customerId.Value, idempotencyKey!, request, cancellationToken);
        return Ok(ApiResponse<WalletTopUpResponseDto>.Success(result, "Wallet top-up created successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("wallet/topups")]
    public async Task<ActionResult<ApiResponse<WalletTopUpPagedResultDto>>> GetMineTopUps(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null) return Unauthorized(ApiResponse<WalletTopUpPagedResultDto>.Fail("Unauthorized access."));
        var result = await walletTopUpService.GetMineAsync(customerId.Value, page, pageSize, cancellationToken);
        return Ok(ApiResponse<WalletTopUpPagedResultDto>.Success(result, "Wallet top-ups retrieved successfully."));
    }

    [HttpGet("wallet/topups/vnpay/return")]
    public async Task<IActionResult> HandleVnPayTopUpReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await walletTopUpService.HandleVnPayReturnAsync(query, cancellationToken);
        return Redirect(result.RedirectUrl);
    }

    [HttpGet("wallet/topups/vnpay/ipn")]
    [Produces("application/json")]
    public async Task<IActionResult> HandleVnPayTopUpIpn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        var result = await walletTopUpService.HandleVnPayIpnAsync(query, cancellationToken);
        return new JsonResult(result, new JsonSerializerOptions { PropertyNamingPolicy = null });
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [EnableRateLimiting("refund-ip")]
    [HttpPost("wallet/withdrawals")]
    public async Task<ActionResult<ApiResponse<WithdrawalResponseDto>>> CreateWithdrawal(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreateWithdrawalRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKey, out _))
            return BadRequest(ApiResponse<WithdrawalResponseDto>.Fail("Idempotency-Key must be a UUID."));
        var customerId = GetCurrentUserId();
        if (customerId is null) return Unauthorized(ApiResponse<WithdrawalResponseDto>.Fail("Unauthorized access."));
        var result = await walletService.CreateWithdrawalAsync(customerId.Value, idempotencyKey!, request, cancellationToken);
        return Accepted(ApiResponse<WithdrawalResponseDto>.Success(result, "Withdrawal request recorded successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Customer))]
    [HttpGet("wallet/withdrawals/me")]
    public async Task<ActionResult<ApiResponse<WithdrawalPagedResultDto>>> GetMineWithdrawals(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null) return Unauthorized(ApiResponse<WithdrawalPagedResultDto>.Fail("Unauthorized access."));
        var result = await walletService.GetMineWithdrawalsAsync(customerId.Value, status, page, pageSize, cancellationToken);
        return Ok(ApiResponse<WithdrawalPagedResultDto>.Success(result, "Withdrawal requests retrieved successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpGet("ops/withdrawals")]
    public async Task<ActionResult<ApiResponse<WithdrawalPagedResultDto>>> GetOperationsWithdrawals(
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await walletService.GetOperationsWithdrawalsAsync(status, page, pageSize, cancellationToken);
        return Ok(ApiResponse<WithdrawalPagedResultDto>.Success(result, "Withdrawal requests retrieved successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpPost("ops/withdrawals/{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<WithdrawalResponseDto>>> CompleteWithdrawal(
        Guid id,
        [FromBody] WithdrawalDecisionDto request,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId is null) return Unauthorized(ApiResponse<WithdrawalResponseDto>.Fail("Unauthorized access."));
        var result = await walletService.CompleteWithdrawalAsync(id, managerId.Value, request, cancellationToken);
        return Ok(ApiResponse<WithdrawalResponseDto>.Success(result, "Bank transfer recorded successfully."));
    }

    [Authorize(Roles = nameof(UserRole.Manager) + "," + nameof(UserRole.Admin))]
    [HttpPost("ops/withdrawals/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<WithdrawalResponseDto>>> RejectWithdrawal(
        Guid id,
        [FromBody] WithdrawalDecisionDto request,
        CancellationToken cancellationToken)
    {
        var managerId = GetCurrentUserId();
        if (managerId is null) return Unauthorized(ApiResponse<WithdrawalResponseDto>.Fail("Unauthorized access."));
        var result = await walletService.RejectWithdrawalAsync(id, managerId.Value, request, cancellationToken);
        return Ok(ApiResponse<WithdrawalResponseDto>.Success(result, "Withdrawal rejected and balance restored."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
