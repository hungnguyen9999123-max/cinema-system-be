using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Movies;

public sealed class RecommendationItem
{
    public Guid MovieId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? PosterUrl { get; set; }

    public string Genre { get; set; } = string.Empty;

    public string AgeRating { get; set; } = string.Empty;

    public int DurationMin { get; set; }

    public double Score { get; set; }

    /// <summary>
    /// Single most important reason in Vietnamese.
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Category of reason: <c>content_genre</c>, <c>content_language</c>,
    /// <c>collab_similar_user</c>, <c>popularity</c>, <c>trending</c>.
    /// </summary>
    public string ReasonType { get; set; } = string.Empty;

    /// <summary>
    /// Top 3 reasons in display order. The first one is also exposed in
    /// <see cref="Reason"/> for compact rendering.
    /// </summary>
    public List<string> Reasons { get; set; } = new();
}

public sealed class RecommendationResponse
{
    /// <summary>
    /// "personalized" if the caller is logged in and has history; "trending" otherwise.
    /// </summary>
    public string Mode { get; set; } = "trending";

    public List<RecommendationItem> Items { get; set; } = new();
}
