using CinemaSystem.Common.DTOs.Payments;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Payments;

public interface IPaymentService
{
    Task<PaymentResponseDto> CreatePaymentAsync(Guid customerId, string idempotencyKey, CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaymentResponseDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, bool isPosStaff = false, CancellationToken cancellationToken = default);
    Task<VnPayIpnResponse> HandleVnPayIpnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default);
    Task<BookingCallbackDto?> GetBookingByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    string BuildVnPayPaymentUrl(DAL.Models.Payment payment, DAL.Models.Booking booking, bool isPosStaff = false);
}

public sealed record VnPayIpnResponse(string RspCode, string Message);
