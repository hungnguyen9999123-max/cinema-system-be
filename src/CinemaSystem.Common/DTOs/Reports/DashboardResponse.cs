namespace CinemaSystem.Common.DTOs.Reports;

/// <summary>
/// Provides the aggregate metrics displayed on the reporting dashboard.
/// </summary>
public class DashboardResponse
{
    /// <summary>
    /// Gets or sets the total value of successful payments.
    /// </summary>
    public decimal TotalRevenue { get; set; }

    /// <summary>
    /// Gets or sets the number of tickets that have not been cancelled.
    /// </summary>
    public int TotalTickets { get; set; }

    /// <summary>
    /// Gets or sets the number of confirmed bookings.
    /// </summary>
    public int TotalBookings { get; set; }

    /// <summary>
    /// Gets or sets the number of active movies.
    /// </summary>
    public int ActiveMovies { get; set; }
}
