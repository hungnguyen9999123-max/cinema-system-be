using CinemaSystem.Common.DTOs.Reports;

namespace CinemaSystem.DAL.Interfaces;

/// <summary>
/// Defines read-only data access operations for reporting and dashboard data.
/// </summary>
public interface IReportRepository
{
    /// <summary>
    /// Gets aggregate metrics for the reporting dashboard.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The dashboard metrics.</returns>
    Task<DashboardResponse> GetDashboardAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets revenue from successful payments, grouped by calendar month.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>Monthly revenue ordered by month.</returns>
    Task<IReadOnlyList<RevenueByMonthResponse>> GetRevenueByMonthAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the five movies with the highest ticket sales.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The five highest-ranked movies by tickets sold.</returns>
    Task<IReadOnlyList<TopMovieResponse>> GetTopMoviesAsync(
        CancellationToken cancellationToken = default);
}
