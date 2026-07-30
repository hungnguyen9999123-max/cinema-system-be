using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Refund
{
    public Guid Id { get; set; }

    public Guid PaymentId { get; set; }

    public Guid? RequestedBy { get; set; }

    public Guid? ProcessedBy { get; set; }

    public decimal RefundAmount { get; set; }

    public string Status { get; set; } = null!;

    public string? GatewayRefundId { get; set; }

    public string? Reason { get; set; }

    public string? ReasonCode { get; set; }

    public string? IdempotencyKeyHash { get; set; }

    public string? DecisionReason { get; set; }

    public string? FailureCode { get; set; }

    public string? FailureMessage { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime? ProcessedAt { get; set; }

    public DateTime? DecidedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? NextReconciliationAt { get; set; }

    public int ReprocessCount { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[]? RowVersion { get; set; }

    public virtual Payment Payment { get; set; } = null!;

    public virtual User? ProcessedByNavigation { get; set; }

    public virtual User? RequestedByNavigation { get; set; }

    public virtual ICollection<RefundGatewayAttempt> GatewayAttempts { get; set; } = new List<RefundGatewayAttempt>();
}
