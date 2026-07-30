namespace CinemaSystem.Common.DTOs.Reports;

/// <summary>
/// Represents a movie ranked by the number of tickets sold.
/// </summary>
public class TopMovieResponse
{
    /// <summary>
    /// Gets or sets the movie title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of tickets sold for the movie.
    /// </summary>
    public int TicketsSold { get; set; }
}
