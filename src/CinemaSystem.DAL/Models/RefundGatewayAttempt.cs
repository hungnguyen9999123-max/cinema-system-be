namespace CinemaSystem.DAL.Models;

public partial class RefundGatewayAttempt
{
    public Guid Id { get; set; }
    public Guid RefundId { get; set; }
    public int AttemptNo { get; set; }
    public string Operation { get; set; } = null!;
    public string MerchantRequestId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? RequestDigest { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? GatewayResponseId { get; set; }
    public string? GatewayTransactionNo { get; set; }
    public string? ResponseCode { get; set; }
    public string? TransactionStatus { get; set; }
    public string? ResponseMessage { get; set; }
    public virtual Refund Refund { get; set; } = null!;
}
