namespace CinemaSystem.Common.DTOs.Reports;

/// <summary>
/// Represents revenue earned during a calendar month.
/// </summary>
public class RevenueByMonthResponse
{
    /// <summary>
    /// Gets or sets the calendar month number, from 1 through 12.
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Gets or sets the total value of successful payments in the month.
    /// </summary>
    public decimal Revenue { get; set; }
}
