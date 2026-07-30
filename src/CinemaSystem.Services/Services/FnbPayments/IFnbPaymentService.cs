using CinemaSystem.Common.DTOs.Payments;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.FnbPayments;

public interface IFnbPaymentService
{
    Task<FnbPaymentResponseDto> CreatePaymentAsync(Guid staffId, CreateFnbPaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<FnbPaymentResponseDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default);
}
