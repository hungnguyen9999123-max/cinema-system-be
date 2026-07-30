namespace CinemaSystem.Common.Enums;

public static class RefundStatus
{
    public const string Requested = "REQUESTED";
    public const string Processing = "PROCESSING";
    public const string ReconciliationRequired = "RECONCILIATION_REQUIRED";
    public const string Succeeded = "SUCCEEDED";
    public const string Rejected = "REJECTED";
    public const string Failed = "FAILED";

    public static readonly ISet<string> Active = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Requested,
        Processing,
        ReconciliationRequired
    };
}
