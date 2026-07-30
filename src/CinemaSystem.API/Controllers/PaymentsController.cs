using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Payments;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Payments;
using CinemaSystem.Services.Services.Wallets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController(IPaymentService paymentService, IWalletTopUpService walletTopUpService, ILogger<PaymentsController> logger) : ControllerBase
{
    /// <summary>
    /// Lấy booking + tickets sau khi thanh toán VNPay (staff POS callback).
    /// Trả về bookingRef, movieTitle, seats, tickets (qr/token) cho staff hiển thị QR cho khách.
    /// </summary>
    [HttpGet("{paymentId:guid}/booking")]
    [ProducesResponseType<ApiResponse<BookingCallbackDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<BookingCallbackDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookingCallbackDto>>> GetBookingByPaymentId(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.GetBookingByPaymentIdAsync(paymentId, cancellationToken);
        if (result == null)
        {
            return NotFound(ApiResponse<BookingCallbackDto>.Fail($"Payment {paymentId} not found."));
        }
        return Ok(ApiResponse<BookingCallbackDto>.Success(result, "Booking retrieved successfully."));
    }

    /// <summary>
    /// Trả về HTML để in QR tickets + F&amp;B receipt cho khách.
    /// Staff POS gọi endpoint này rồi render vào cửa sổ print riêng.
    /// </summary>
    [HttpGet("{paymentId:guid}/booking/print")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> GetBookingPrintByPaymentId(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result = await paymentService.GetBookingByPaymentIdAsync(paymentId, cancellationToken);
        if (result == null)
        {
            return NotFound($"Payment {paymentId} not found.");
        }

        var html = BuildPrintHtml(result);
        return Content(html, "text/html; charset=utf-8");
    }

    private static string BuildPrintHtml(BookingCallbackDto b)
    {
        var ticketCards = string.Join("\n", b.Tickets.Select(t => $@"
            <div class=""ticket-card"">
                <div class=""ticket-header"">
                    <h3>{b.MovieTitle}</h3>
                    <p class=""booking-ref"">#{b.BookingRef}</p>
                </div>
                <div class=""ticket-body"">
                    <div class=""ticket-info"">
                        <div class=""info-row""><strong>Rạp:</strong> {b.CinemaName}</div>
                        <div class=""info-row""><strong>Phòng:</strong> {b.RoomName}</div>
                        <div class=""info-row""><strong>Ghế:</strong> {t.SeatLabel}</div>
                        <div class=""info-row""><strong>Suất chiếu:</strong> {b.ShowtimeStart:dd/MM/yyyy HH:mm} - {b.ShowtimeEnd:HH:mm}</div>
                    </div>
                    <div class=""qr-section"">
                        <img src=""data:image/png;base64,{t.QrImageBase64}"" alt=""QR Code"" class=""qr-image"" />
                        <p class=""token"">{t.Token}</p>
                    </div>
                </div>
            </div>"));

        var fnbSection = b.FnbOrders.Count > 0
            ? $@"
            <div class=""fnb-section"">
                <h3>F&amp;B</h3>
                <table class=""fnb-table"">
                    <thead><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr></thead>
                    <tbody>
                        {string.Join("\n", b.FnbOrders.SelectMany(o => o.Items).Select(i => $@"
                        <tr>
                            <td>{i.ItemName}</td>
                            <td>{i.Quantity}</td>
                            <td>{i.UnitPrice:N0}đ</td>
                            <td>{i.Subtotal:N0}đ</td>
                        </tr>"))}
                    </tbody>
                </table>
                <p class=""fnb-total"">Tổng F&amp;B: <strong>{b.FnbTotalAmount:N0}đ</strong></p>
            </div>"
            : string.Empty;

        return $@"<!DOCTYPE html>
<html lang=""vi"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>In vé #{b.BookingRef}</title>
<style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    body {{ font-family: Arial, sans-serif; padding: 20px; color: #333; font-size: 13px; }}
    .receipt-header {{ text-align: center; margin-bottom: 16px; padding-bottom: 12px; border-bottom: 2px dashed #ccc; }}
    .receipt-header h2 {{ color: #1a1a1a; }}
    .receipt-header p {{ color: #666; font-size: 12px; }}
    .summary {{ margin: 16px 0; padding: 10px; background: #f5f5f5; border-radius: 6px; }}
    .summary-row {{ display: flex; justify-content: space-between; margin-bottom: 4px; }}
    .summary-row.total {{ font-weight: bold; font-size: 16px; border-top: 1px solid #ccc; padding-top: 6px; margin-top: 6px; }}
    .ticket-card {{
        border: 1px solid #ddd; border-radius: 8px; margin-bottom: 20px; page-break-inside: avoid;
        box-shadow: 0 2px 6px rgba(0,0,0,.06);
    }}
    .ticket-header {{ background: #1a1a1a; color: #fff; padding: 12px 16px; border-radius: 8px 8px 0 0; }}
    .ticket-header h3 {{ font-size: 16px; margin-bottom: 4px; }}
    .booking-ref {{ font-size: 12px; color: #ccc; }}
    .ticket-body {{ display: flex; padding: 16px; gap: 16px; }}
    .ticket-info {{ flex: 1; }}
    .info-row {{ margin-bottom: 6px; }}
    .qr-section {{ text-align: center; }}
    .qr-image {{ width: 140px; height: 140px; border: 1px solid #eee; }}
    .token {{ font-size: 10px; color: #888; margin-top: 6px; word-break: break-all; }}
    .fnb-section {{ margin-top: 20px; padding: 16px; background: #f9f9f9; border-radius: 8px; }}
    .fnb-section h3 {{ margin-bottom: 10px; font-size: 14px; }}
    .fnb-table {{ width: 100%; border-collapse: collapse; font-size: 12px; }}
    .fnb-table th, .fnb-table td {{ padding: 6px 8px; text-align: left; border-bottom: 1px solid #eee; }}
    .fnb-table th {{ background: #eee; font-weight: 600; }}
    .fnb-total {{ text-align: right; margin-top: 8px; font-size: 14px; }}
    .print-btn {{
        display: block; margin: 20px auto; padding: 10px 24px;
        background: #1a1a1a; color: #fff; border: none; border-radius: 6px;
        cursor: pointer; font-size: 14px;
    }}
    @media print {{
        .print-btn {{ display: none; }}
        .ticket-card {{ page-break-inside: avoid; }}
    }}
</style>
</head>
<body>
<button class=""print-btn"" onclick=""window.print()"">🖨️ In vé</button>

<div class=""receipt-header"">
    <h2>VÉ XEM PHIM</h2>
    <p>{b.CinemaName} · {b.RoomName}</p>
</div>

<div class=""summary"">
    <div class=""summary-row""><span>Vé ({b.Tickets.Count} ghế)</span><span>{b.TotalAmount:N0}đ</span></div>
    {(b.DiscountAmount > 0 ? $@"<div class=""summary-row""><span>Giảm giá</span><span>-{b.DiscountAmount:N0}đ</span></div>" : "")}
    {(b.FnbTotalAmount > 0 ? $@"<div class=""summary-row""><span>F&amp;B</span><span>{b.FnbTotalAmount:N0}đ</span></div>" : "")}
    <div class=""summary-row total""><span>TỔNG THANH TOÁN</span><span>{b.FinalAmount:N0}đ</span></div>
</div>

{ticketCards}
{fnbSection}
</body>
</html>";
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PaymentResponseDto>>> CreatePayment(
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        [FromBody] CreatePaymentRequestDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<PaymentResponseDto>.Fail("Unauthorized access."));
        }
        if (!Guid.TryParse(idempotencyKey, out _))
        {
            return BadRequest(ApiResponse<PaymentResponseDto>.Fail("Idempotency-Key must be a UUID."));
        }

        var response = await paymentService.CreatePaymentAsync(customerId.Value, idempotencyKey!, request, cancellationToken);
        return Ok(ApiResponse<PaymentResponseDto>.Success(response, "Payment created successfully."));
    }

    [HttpGet("vnpay/return")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandleVnPayReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        logger.LogDebug(
            "VNPay return received - keys: [{Keys}], secure hash present: {HasSecureHash}, transaction reference: {TxnRef}",
            string.Join(", ", query.Keys),
            query.ContainsKey("vnp_securehash"),
            query.TryGetValue("vnp_txnref", out var tx) ? tx : "NULL");

        var response = await paymentService.HandleVnPayReturnAsync(query, isPosStaff: false, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return Redirect(response.RedirectUrl);
        }

        return Ok(ApiResponse<PaymentResponseDto>.Success(
            response,
            "VNPay return handled successfully."));
    }

    [HttpGet("vnpay/pos-return")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<PaymentResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HandleVnPayPosReturn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        logger.LogDebug(
            "VNPay POS return received - keys: [{Keys}], orderInfo={OrderInfo}",
            string.Join(", ", query.Keys),
            query.TryGetValue("vnp_orderinfo", out var oi) ? oi : "NULL");

        var response = await paymentService.HandleVnPayReturnAsync(query, isPosStaff: true, cancellationToken);

        if (!string.IsNullOrWhiteSpace(response.RedirectUrl))
        {
            return Redirect(response.RedirectUrl);
        }

        return Ok(ApiResponse<PaymentResponseDto>.Success(
            response,
            "VNPay POS return handled successfully."));
    }

    [HttpGet("vnpay/ipn")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> HandleVnPayIpn(CancellationToken cancellationToken)
    {
        var query = Request.Query.ToDictionary(
            item => item.Key,
            item => item.Value.ToString(),
            StringComparer.OrdinalIgnoreCase);

        var isWalletTopUp = query.TryGetValue("vnp_orderinfo", out var orderInfo)
            && orderInfo.StartsWith("Nap vi CINE MAX", StringComparison.OrdinalIgnoreCase);
        var response = isWalletTopUp
            ? await walletTopUpService.HandleVnPayIpnAsync(query, cancellationToken)
            : await paymentService.HandleVnPayIpnAsync(query, cancellationToken);

        return new JsonResult(response, new JsonSerializerOptions { PropertyNamingPolicy = null });
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
