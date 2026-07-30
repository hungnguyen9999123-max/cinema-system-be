using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Payments;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.FnbPayments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/payments/fnb")]
public sealed class FnbPaymentsController(IFnbPaymentService fnbPaymentService, ILogger<FnbPaymentsController> logger) : ControllerBase
{
    [Authorize(Roles = "Admin,Manager,Staff")]
    [HttpPost]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FnbPaymentResponseDto>>> CreatePayment(
        [FromBody] CreateFnbPaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId == Guid.Empty)
        {
            return BadRequest(ApiResponse<FnbPaymentResponseDto>.Fail("Invalid token."));
        }

        var response = await fnbPaymentService.CreatePaymentAsync(staffId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse<FnbPaymentResponseDto>.Success(response, "F&B payment created successfully."));
    }

    [HttpGet("vnpay/return")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleVnPayReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        logger.LogDebug("F&B VNPay return received - keys: [{Keys}]", string.Join(", ", query.Keys));

        var response = await fnbPaymentService.HandleVnPayReturnAsync(query, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return Redirect(response.RedirectUrl);
        }

        return Ok(ApiResponse<FnbPaymentResponseDto>.Success(response, "F&B payment processed successfully."));
    }

    [HttpGet("vnpay/pos-return")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<FnbPaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleVnPayPosReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        logger.LogDebug("F&B VNPay POS return received - keys: [{Keys}]", string.Join(", ", query.Keys));

        var response = await fnbPaymentService.HandleVnPayReturnAsync(query, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return Redirect(response.RedirectUrl);
        }

        return Ok(ApiResponse<FnbPaymentResponseDto>.Success(response, "F&B POS payment processed successfully."));
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(raw, out var userId) ? userId : Guid.Empty;
    }
}
