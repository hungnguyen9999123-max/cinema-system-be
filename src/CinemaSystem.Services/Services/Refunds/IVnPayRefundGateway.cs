using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.Refunds;

/// <summary>
/// Boundary around VNPAY's QueryDR/Refund API.  Keeping this boundary separate
/// means the transaction state machine is testable without calling the gateway.
/// </summary>
public interface IVnPayRefundGateway
{
    Task<VnPayRefundGatewayResult> RefundAsync(Refund refund, Payment payment, string requestId, CancellationToken cancellationToken = default);
    Task<VnPayRefundGatewayResult> QueryAsync(Refund refund, Payment payment, string requestId, CancellationToken cancellationToken = default);
}

public sealed record VnPayRefundGatewayResult(
    bool IsTransportSuccess,
    bool IsSignatureValid,
    string ResponseCode,
    string? TransactionStatus,
    string? TransactionNo,
    string? GatewayRequestId,
    string? Message,
    string ResponseDigest)
{
    public bool IsAccepted => IsTransportSuccess && IsSignatureValid && ResponseCode == "00";
    public bool IsRefundSucceeded => IsAccepted && (string.IsNullOrWhiteSpace(TransactionStatus) || TransactionStatus == "00");
}
