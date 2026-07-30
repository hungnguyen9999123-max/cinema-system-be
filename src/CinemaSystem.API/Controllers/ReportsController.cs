using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Reports;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Reports;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

/// <summary>
/// Provides read-only reporting and dashboard endpoints.
/// </summary>
[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    /// <summary>
    /// Gets aggregate revenue, ticket, booking, and active-movie metrics.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The current dashboard metrics.</returns>
    [HttpGet("dashboard")]
    [ProducesResponseType<ApiResponse<DashboardResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<DashboardResponse>>> GetDashboard(
        CancellationToken cancellationToken)
    {
        var dashboard = await reportService.GetDashboardAsync(cancellationToken);
        return Ok(ApiResponse<DashboardResponse>.Success(dashboard, CommonMessages.Retrieved));
    }

    /// <summary>
    /// Gets successful-payment revenue grouped by calendar month.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>Monthly revenue ordered by month.</returns>
    [HttpGet("revenue-by-month")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<RevenueByMonthResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RevenueByMonthResponse>>>> GetRevenueByMonth(
        CancellationToken cancellationToken)
    {
        var revenueByMonth = await reportService.GetRevenueByMonthAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RevenueByMonthResponse>>.Success(
            revenueByMonth,
            CommonMessages.Retrieved));
    }

    /// <summary>
    /// Gets the five movies with the highest ticket sales.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the request.</param>
    /// <returns>The top movies ordered by tickets sold.</returns>
    [HttpGet("top-movies")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<TopMovieResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TopMovieResponse>>>> GetTopMovies(
        CancellationToken cancellationToken)
    {
        var topMovies = await reportService.GetTopMoviesAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<TopMovieResponse>>.Success(
            topMovies,
            CommonMessages.Retrieved));
    }
}
