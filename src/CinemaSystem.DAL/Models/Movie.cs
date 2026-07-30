using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Movie
{
    public Guid Id { get; set; }

    public Guid CreatedBy { get; set; }

    public string Title { get; set; } = null!;

    public string Genre { get; set; } = null!;

    public string Language { get; set; } = null!;

    public int DurationMin { get; set; }

    public DateOnly ReleaseDate { get; set; }

    public string? Synopsis { get; set; }

    public string AgeRating { get; set; } = null!;

    public string? PosterUrl { get; set; }

    public string? TrailerUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? PosterPublicId { get; set; }

    public string? BannerUrl { get; set; }

    public string? BannerPublicId { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
