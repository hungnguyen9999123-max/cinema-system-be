using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CinemaSystem.DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.Refunds;

public sealed class VnPayRefundGateway(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<VnPayRefundGateway> logger) : IVnPayRefundGateway
{
    private const string VnPayVersion = "2.1.0";
    private const string RefundCommand = "refund";
    private const string QueryCommand = "querydr";
    private readonly VnPayRefundOptions options = VnPayRefundOptions.FromConfiguration(configuration);

    public Task<VnPayRefundGatewayResult> RefundAsync(Refund refund, Payment payment, string requestId, CancellationToken cancellationToken = default) =>
        SendAsync(refund, payment, requestId, RefundCommand, cancellationToken);

    public Task<VnPayRefundGatewayResult> QueryAsync(Refund refund, Payment payment, string requestId, CancellationToken cancellationToken = default) =>
        SendAsync(refund, payment, requestId, QueryCommand, cancellationToken);

    private async Task<VnPayRefundGatewayResult> SendAsync(
        Refund refund,
        Payment payment,
        string requestId,
        string command,
        CancellationToken cancellationToken)
    {
        options.EnsureConfigured();

        var transactionDate = ToVnPayTime(payment.GatewayRequestAt ?? payment.CreatedAt);
        var createDate = ToVnPayTime(DateTime.UtcNow);
        var transactionNo = payment.GatewayTxnId ?? string.Empty;
        var orderInfo = $"Refund booking {payment.Booking.BookingRef}";
        var amount = decimal.Truncate(refund.RefundAmount * 100m).ToString("0", CultureInfo.InvariantCulture);

        var fields = command == RefundCommand
            ? new Dictionary<string, string>
            {
                ["vnp_RequestId"] = requestId,
                ["vnp_Version"] = VnPayVersion,
                ["vnp_Command"] = RefundCommand,
                ["vnp_TmnCode"] = options.TmnCode,
                ["vnp_TransactionType"] = "02",
                ["vnp_TxnRef"] = payment.Id.ToString("N"),
                ["vnp_Amount"] = amount,
                ["vnp_TransactionNo"] = transactionNo,
                ["vnp_TransactionDate"] = transactionDate,
                ["vnp_CreateBy"] = refund.ProcessedBy?.ToString("N") ?? "system",
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = options.IpAddress,
                ["vnp_OrderInfo"] = orderInfo
            }
            : new Dictionary<string, string>
            {
                ["vnp_RequestId"] = requestId,
                ["vnp_Version"] = VnPayVersion,
                ["vnp_Command"] = QueryCommand,
                ["vnp_TmnCode"] = options.TmnCode,
                ["vnp_TxnRef"] = payment.Id.ToString("N"),
                ["vnp_TransactionDate"] = transactionDate,
                ["vnp_CreateDate"] = createDate,
                ["vnp_IpAddr"] = options.IpAddress,
                ["vnp_OrderInfo"] = orderInfo
            };

        fields["vnp_SecureHash"] = ComputeHash(BuildRequestSignatureData(fields, command));
        var requestDigest = ComputeSha256(JsonSerializer.Serialize(fields));

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, options.ApiUrl)
            {
                Content = JsonContent.Create(fields)
            };
            using var response = await httpClientFactory.CreateClient("VnPayRefund").SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var data = ParseResponse(content);
            var responseCode = GetValue(data, "vnp_ResponseCode") ?? "99";
            var responseHash = GetValue(data, "vnp_SecureHash") ?? string.Empty;
            var validSignature = !string.IsNullOrEmpty(responseHash) &&
                FixedTimeEquals(responseHash, ComputeHash(BuildResponseSignatureData(data, command)));

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("VNPAY {Command} returned HTTP {StatusCode}; request {RequestId}.", command, (int)response.StatusCode, requestId);
            }

            return new VnPayRefundGatewayResult(
                response.IsSuccessStatusCode,
                validSignature,
                responseCode,
                GetValue(data, "vnp_TransactionStatus"),
                GetValue(data, "vnp_TransactionNo"),
                GetValue(data, "vnp_ResponseId"),
                GetValue(data, "vnp_Message") ??
                    GetValue(data, "vnp_TransactionStatus") ??
                    (!response.IsSuccessStatusCode ? $"VNPAY returned HTTP {(int)response.StatusCode}." : null),
                ComputeSha256(content));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new VnPayRefundGatewayResult(false, false, "TIMEOUT", null, null, null, "VNPAY did not return a result within 30 seconds.", requestDigest);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "VNPAY {Command} could not be reached; request {RequestId}.", command, requestId);
            return new VnPayRefundGatewayResult(false, false, "NETWORK_ERROR", null, null, null, "Unable to reach VNPAY.", requestDigest);
        }
    }

    private static Dictionary<string, string> ParseResponse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().ToDictionary(item => item.Name, item => item.Value.ToString(), StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static string BuildRequestSignatureData(IReadOnlyDictionary<string, string> values, string command)
    {
        var orderedKeys = command == RefundCommand
            ? new[]
            {
                "vnp_RequestId", "vnp_Version", "vnp_Command", "vnp_TmnCode", "vnp_TransactionType", "vnp_TxnRef",
                "vnp_Amount", "vnp_TransactionNo", "vnp_TransactionDate", "vnp_CreateBy", "vnp_CreateDate", "vnp_IpAddr", "vnp_OrderInfo"
            }
            : new[]
            {
                "vnp_RequestId", "vnp_Version", "vnp_Command", "vnp_TmnCode", "vnp_TxnRef", "vnp_TransactionDate",
                "vnp_CreateDate", "vnp_IpAddr", "vnp_OrderInfo"
            };
        return string.Join("|", orderedKeys.Select(key => GetValue(values, key) ?? string.Empty));
    }

    private static string BuildResponseSignatureData(IReadOnlyDictionary<string, string> values, string command)
    {
        var orderedKeys = command == RefundCommand
            ? new[]
            {
                "vnp_ResponseId", "vnp_Command", "vnp_ResponseCode", "vnp_Message", "vnp_TmnCode", "vnp_TxnRef", "vnp_Amount",
                "vnp_BankCode", "vnp_PayDate", "vnp_TransactionNo", "vnp_TransactionType", "vnp_TransactionStatus", "vnp_OrderInfo"
            }
            : new[]
            {
                "vnp_ResponseId", "vnp_Command", "vnp_ResponseCode", "vnp_Message", "vnp_TmnCode", "vnp_TxnRef", "vnp_Amount",
                "vnp_BankCode", "vnp_PayDate", "vnp_TransactionNo", "vnp_TransactionType", "vnp_TransactionStatus", "vnp_OrderInfo",
                "vnp_PromotionCode", "vnp_PromotionAmount"
            };
        return string.Join("|", orderedKeys.Select(key => GetValue(values, key) ?? string.Empty));
    }

    private string ComputeHash(string data)
    {
        var key = Encoding.UTF8.GetBytes(options.HashSecret);
        var bytes = HMACSHA512.HashData(key, Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ComputeSha256(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static bool FixedTimeEquals(string left, string right) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(left.ToLowerInvariant()), Encoding.UTF8.GetBytes(right.ToLowerInvariant()));

    // Application timestamps are persisted as UTC. SQL Server returns them as
    // Unspecified, so always translate once to the VNPAY-required GMT+7 value.
    private static string ToVnPayTime(DateTime value) =>
        value.AddHours(7).ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture);

    private sealed class VnPayRefundOptions
    {
        public string TmnCode { get; private init; } = string.Empty;
        public string HashSecret { get; private init; } = string.Empty;
        public string ApiUrl { get; private init; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
        public string IpAddress { get; private init; } = "127.0.0.1";

        public static VnPayRefundOptions FromConfiguration(IConfiguration configuration)
        {
            var vnpay = configuration.GetSection("VnPay");
            return new VnPayRefundOptions
            {
                TmnCode = (vnpay["TmnCode"] ?? string.Empty).Trim(),
                HashSecret = (vnpay["HashSecret"] ?? string.Empty).Trim(),
                ApiUrl = (configuration["VnPayRefund:ApiUrl"] ?? "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction").Trim(),
                IpAddress = (configuration["VnPayRefund:IpAddress"] ?? "127.0.0.1").Trim()
            };
        }

        public void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(TmnCode) ||
                string.IsNullOrWhiteSpace(HashSecret) ||
                string.IsNullOrWhiteSpace(ApiUrl) ||
                !System.Net.IPAddress.TryParse(IpAddress, out _))
            {
                throw new InvalidOperationException("VNPay refund configuration is missing.");
            }
        }
    }
}
