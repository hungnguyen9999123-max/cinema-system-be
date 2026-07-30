using CinemaSystem.Common.DTOs.Wallets;
using CinemaSystem.Services.Services.Payments;

namespace CinemaSystem.Services.Services.Wallets;

public interface IWalletTopUpService
{
    Task<WalletTopUpResponseDto> CreateAsync(Guid customerId, string idempotencyKey, CreateWalletTopUpRequestDto request, CancellationToken cancellationToken = default);
    Task<WalletTopUpPagedResultDto> GetMineAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WalletTopUpCallbackResult> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default);
    Task<VnPayIpnResponse> HandleVnPayIpnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default);
}

public sealed record WalletTopUpCallbackResult(WalletTopUpResponseDto Response, bool AlreadyProcessed, string RedirectUrl);
