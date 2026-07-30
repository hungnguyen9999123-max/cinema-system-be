using CinemaSystem.Common.DTOs.Reports;
using CinemaSystem.DAL.Interfaces;

namespace CinemaSystem.Services.Services.Reports;

/// <summary>
/// Coordinates retrieval of reporting and dashboard data.
/// </summary>
public sealed class ReportService(IReportRepository reportRepository) : IReportService
{
    /// <inheritdoc />
    public async Task<DashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        return await reportRepository.GetDashboardAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RevenueByMonthResponse>> GetRevenueByMonthAsync(
        CancellationToken cancellationToken = default)
    {
        return await reportRepository.GetRevenueByMonthAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<TopMovieResponse>> GetTopMoviesAsync(
        CancellationToken cancellationToken = default)
    {
        return await reportRepository.GetTopMoviesAsync(cancellationToken);
    }
}
