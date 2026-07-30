using CinemaSystem.Common.DTOs.Movies;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Recommendations;

public interface IRecommendationService
{
    Task<RecommendationResponse> GetRecommendationsAsync(
        Guid? customerId,
        int limit,
        CancellationToken cancellationToken = default);
}

public sealed class RecommendationService(
    IMovieRepository movieRepository,
    CinemaDbContext dbContext) : IRecommendationService
{
    private const string BookingConfirmed = "CONFIRMED";
    private const double ContentWeight = 0.6;
    private const double CollaborativeWeight = 0.4;
    private const int NeighborLimit = 10;
    private const int MinNeighbors = 1;
    private const double PopularityBoost = 0.05;

    public async Task<RecommendationResponse> GetRecommendationsAsync(
        Guid? customerId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        if (limit <= 0) limit = 10;
        if (limit > 30) limit = 30;

        var now = DateTime.Now;
        var futureShowtimeMovies = await movieRepository.GetActiveMoviesWithFutureShowtimesAsync(now, cancellationToken);
        if (futureShowtimeMovies.Count == 0)
        {
            return new RecommendationResponse { Mode = "trending", Items = new() };
        }

        // --- Gather user behaviour once ---
        HashSet<Guid> watchedMovieIds = new();
        List<Feedback> ratedFeedbacks = new();
        Dictionary<string, double> userProfile = new(StringComparer.Ordinal);

        if (customerId.HasValue && customerId.Value != Guid.Empty)
        {
            var userBookings = await dbContext.Bookings
                .AsNoTracking()
                .Where(b => b.CustomerId == customerId.Value
                            && (b.Status == BookingConfirmed))
                .Select(b => new { b.Showtime!.MovieId, b.ShowtimeId })
                .ToListAsync(cancellationToken);

            watchedMovieIds = userBookings.Select(x => x.MovieId).Distinct().ToHashSet();

            ratedFeedbacks = await dbContext.Feedbacks
                .AsNoTracking()
                .Where(f => f.CustomerId == customerId.Value)
                .ToListAsync(cancellationToken);

            // Build a weighted user vector (only rated, with rating as weight).
            foreach (var fb in ratedFeedbacks)
            {
                var movie = futureShowtimeMovies.FirstOrDefault(m => m.Id == fb.MovieId);
                if (movie is null) continue;

                var movieVector = MovieVector.Build(movie);
                foreach (var (k, v) in movieVector)
                {
                    userProfile[k] = userProfile.GetValueOrDefault(k) + v * fb.Rating;
                }
            }

            // Normalize profile
            if (userProfile.Count > 0)
            {
                var norm = Math.Sqrt(userProfile.Sum(kv => kv.Value * kv.Value));
                if (norm > 0)
                {
                    foreach (var key in userProfile.Keys.ToList())
                    {
                        userProfile[key] /= norm;
                    }
                }
            }
        }

        // --- Build candidate set (movies user has not watched yet) ---
        var candidates = futureShowtimeMovies
            .Where(m => !watchedMovieIds.Contains(m.Id))
            .ToList();

        if (candidates.Count == 0)
        {
            // User has watched everything with future showtimes — fall back to trending.
            return await BuildTrendingAsync(futureShowtimeMovies, limit, cancellationToken);
        }

        // --- Cold-start: no user history -> trending ---
        if (!customerId.HasValue || customerId.Value == Guid.Empty
            || (ratedFeedbacks.Count == 0 && watchedMovieIds.Count == 0))
        {
            return await BuildTrendingAsync(candidates, limit, cancellationToken);
        }

        // --- Collaborative signals (neighbors) ---
        var neighborScores = await ComputeNeighborScoresAsync(
            customerId.Value,
            candidates,
            cancellationToken);

        // --- Precompute aggregate ratings and popularity ---
        var ratingAggregates = await movieRepository.GetMovieRatingAggregatesAsync(cancellationToken);
        var ratingMap = ratingAggregates.ToDictionary(x => x.MovieId, x => (x.AverageRating, x.FeedbackCount));

        var trending = (await movieRepository.GetTopMoviesByConfirmedBookingsAsync(200, cancellationToken))
            .ToDictionary(x => x.MovieId, x => x.BookingCount);

        // --- Score each candidate ---
        var maxPopularity = Math.Max(1, trending.Values.DefaultIfEmpty(0).Max());

        var scored = candidates.Select(movie =>
        {
            var movieVector = MovieVector.Build(movie);

            // Content score
            double contentScore = 0;
            if (userProfile.Count > 0)
            {
                contentScore = CosineSimilarity(userProfile, movieVector);
            }

            // Collaborative score
            double collabScore = 0;
            if (neighborScores.TryGetValue(movie.Id, out var cs))
            {
                collabScore = cs;
            }

            // Fallback: if user has ratings but no content signal, let collaborative dominate.
            var cWeight = userProfile.Count > 0 ? ContentWeight : 0.0;
            var collWeight = userProfile.Count > 0 ? CollaborativeWeight : 1.0;

            double raw = (cWeight * contentScore) + (collWeight * collabScore);

            // Popularity boost (gentle)
            if (trending.TryGetValue(movie.Id, out var pv))
            {
                raw += PopularityBoost * Math.Log10(pv + 1) / Math.Log10(maxPopularity + 1);
            }

            // Rating boost
            if (ratingMap.TryGetValue(movie.Id, out var rm))
            {
                // Map rating 1-5 to 0-0.10; multiply by feedback count normalized to 0.5
                var ratingBoost = ((rm.AverageRating - 3.0) / 2.0) * 0.10 * Math.Min(1.0, Math.Log10(rm.FeedbackCount + 1) / 2.0);
                raw += ratingBoost;
            }

            return new ScoredMovie
            {
                Movie = movie,
                ContentScore = contentScore,
                CollabScore = collabScore,
                FinalScore = raw,
            };
        }).ToList();

        // --- Pick top-K and explain ---
        var top = scored
            .OrderByDescending(s => s.FinalScore)
            .ThenBy(s => s.Movie.Title)
            .Take(limit)
            .ToList();

        var items = top.Select(s => BuildItem(s, ratingMap, trending)).ToList();

        return new RecommendationResponse
        {
            Mode = "personalized",
            Items = items,
        };
    }

    private async Task<RecommendationResponse> BuildTrendingAsync(
        List<Movie> candidates,
        int limit,
        CancellationToken cancellationToken)
    {
        var ratingAggregates = await movieRepository.GetMovieRatingAggregatesAsync(cancellationToken);
        var ratingMap = ratingAggregates.ToDictionary(x => x.MovieId, x => (x.AverageRating, x.FeedbackCount));

        var trending = (await movieRepository.GetTopMoviesByConfirmedBookingsAsync(200, cancellationToken))
            .ToDictionary(x => x.MovieId, x => x.BookingCount);

        var maxPopularity = Math.Max(1, trending.Values.DefaultIfEmpty(0).Max());

        var scored = candidates.Select(m =>
        {
            double score = 0;
            if (trending.TryGetValue(m.Id, out var pv))
            {
                score += 0.7 * Math.Log10(pv + 1) / Math.Log10(maxPopularity + 1);
            }
            if (ratingMap.TryGetValue(m.Id, out var rm))
            {
                score += 0.3 * (rm.AverageRating / 5.0);
            }
            return new ScoredMovie { Movie = m, FinalScore = score };
        })
        .OrderByDescending(s => s.FinalScore)
        .Take(limit)
        .ToList();

        var items = scored.Select(s => BuildItem(s, ratingMap, trending)).ToList();

        return new RecommendationResponse
        {
            Mode = "trending",
            Items = items,
        };
    }

    private async Task<Dictionary<Guid, double>> ComputeNeighborScoresAsync(
        Guid customerId,
        List<Movie> candidates,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<Guid, double>();

        // 1. Find movies the user has positively rated (>= 4) or watched to use
        //    as the "taste seed" for neighbor search.
        var userMovieIds = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.Status == BookingConfirmed)
            .Select(b => b.Showtime!.MovieId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var positiveFeedbackMovieIds = await dbContext.Feedbacks
            .AsNoTracking()
            .Where(f => f.CustomerId == customerId && f.Rating >= 4)
            .Select(f => f.MovieId)
            .ToListAsync(cancellationToken);

        var seedMovieIds = userMovieIds.Union(positiveFeedbackMovieIds).Distinct().ToList();
        if (seedMovieIds.Count == 0)
        {
            return result;
        }

        // 2. Find candidate users: anyone who watched the same movies.
        var candidateUserIds = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingConfirmed
                        && b.CustomerId != customerId
                        && seedMovieIds.Contains(b.Showtime!.MovieId))
            .Select(b => b.CustomerId)
            .Distinct()
            .Take(500) // pre-limit to avoid huge joins
            .ToListAsync(cancellationToken);

        if (candidateUserIds.Count == 0)
        {
            return result;
        }

        // 3. Build co-watched counts per neighbor.
        var coWatchCounts = await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingConfirmed
                        && candidateUserIds.Contains(b.CustomerId)
                        && seedMovieIds.Contains(b.Showtime!.MovieId))
            .GroupBy(b => b.CustomerId)
            .Select(g => new { CustomerId = g.Key, CoCount = g.Count() })
            .ToListAsync(cancellationToken);

        var topNeighborIds = coWatchCounts
            .OrderByDescending(x => x.CoCount)
            .Take(NeighborLimit)
            .Select(x => x.CustomerId)
            .ToList();

        if (topNeighborIds.Count < MinNeighbors)
        {
            return result;
        }

        // 4. Aggregate feedback the neighbors gave to the candidate movies.
        var candidateIds = candidates.Select(c => c.Id).ToHashSet();

        var neighborFeedback = await dbContext.Feedbacks
            .AsNoTracking()
            .Where(f => topNeighborIds.Contains(f.CustomerId)
                        && candidateIds.Contains(f.MovieId))
            .Select(f => new { f.MovieId, f.Rating, f.CustomerId })
            .ToListAsync(cancellationToken);

        // 5. Compute Jaccard similarity once per neighbor for weighting.
        var weightByNeighbor = new Dictionary<Guid, double>();
        foreach (var neighborId in topNeighborIds)
        {
            var seedNeighborOverlap = await dbContext.Bookings
                .AsNoTracking()
                .CountAsync(b => b.CustomerId == neighborId
                                 && b.Status == BookingConfirmed
                                 && seedMovieIds.Contains(b.Showtime!.MovieId), cancellationToken);

            var neighborTotal = await dbContext.Bookings
                .AsNoTracking()
                .CountAsync(b => b.CustomerId == neighborId && b.Status == BookingConfirmed, cancellationToken);

            var union = seedMovieIds.Count + neighborTotal - seedNeighborOverlap;
            var jaccard = union > 0 ? (double)seedNeighborOverlap / union : 0;
            weightByNeighbor[neighborId] = jaccard;
        }

        // 6. Aggregate weighted rating per candidate movie.
        var grouped = neighborFeedback
            .GroupBy(f => f.MovieId)
            .Select(g => new
            {
                MovieId = g.Key,
                WeightedSum = g.Sum(f => f.Rating * weightByNeighbor.GetValueOrDefault(f.CustomerId, 0)),
                WeightSum = g.Sum(f => weightByNeighbor.GetValueOrDefault(f.CustomerId, 0)),
            })
            .Where(x => x.WeightSum > 0)
            .ToList();

        foreach (var entry in grouped)
        {
            // Normalize to 0..1 by dividing by (5 * maxWeight)
            var normalized = entry.WeightedSum / (5.0 * Math.Max(0.0001, entry.WeightSum));
            result[entry.MovieId] = Math.Clamp(normalized, 0, 1);
        }

        return result;
    }

    private static RecommendationItem BuildItem(
        ScoredMovie scored,
        Dictionary<Guid, (double AverageRating, int FeedbackCount)> ratingMap,
        Dictionary<Guid, int> trending)
    {
        var reasons = new List<string>();
        var primaryType = "popularity";

        if (scored.ContentScore > 0.4)
        {
            reasons.Add($"Cùng thể loại {scored.Movie.Genre} mà bạn hay xem");
            primaryType = "content_genre";
        }
        else if (scored.ContentScore > 0.15)
        {
            reasons.Add($"Phim có nội dung tương tự sở thích của bạn");
            primaryType = "content_genre";
        }

        if (scored.CollabScore > 0.5)
        {
            reasons.Add($"Khách hàng có sở thích giống bạn đã thích phim này");
            primaryType = primaryType == "content_genre" ? primaryType : "collab_similar_user";
        }

        if (trending.TryGetValue(scored.Movie.Id, out var pv) && pv > 5)
        {
            reasons.Add("Đang được xem nhiều tuần này");
            if (primaryType == "popularity") primaryType = "trending";
        }

        if (ratingMap.TryGetValue(scored.Movie.Id, out var rm) && rm.AverageRating >= 4.0 && rm.FeedbackCount >= 3)
        {
            reasons.Add($"Được {rm.FeedbackCount} lượt đánh giá trung bình {rm.AverageRating:F1}/5");
        }

        if (reasons.Count == 0)
        {
            reasons.Add("Phim mới đáng chú ý");
        }

        return new RecommendationItem
        {
            MovieId = scored.Movie.Id,
            Title = scored.Movie.Title,
            PosterUrl = scored.Movie.PosterUrl,
            Genre = scored.Movie.Genre,
            AgeRating = scored.Movie.AgeRating,
            DurationMin = scored.Movie.DurationMin,
            Score = Math.Round(scored.FinalScore, 4),
            Reason = reasons[0],
            ReasonType = primaryType,
            Reasons = reasons.Take(3).ToList(),
        };
    }

    private static double CosineSimilarity(Dictionary<string, double> a, Dictionary<string, double> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0;

        double dot = 0;
        foreach (var (k, av) in a)
        {
            if (b.TryGetValue(k, out var bv))
            {
                dot += av * bv;
            }
        }

        var normA = Math.Sqrt(a.Sum(kv => kv.Value * kv.Value));
        var normB = Math.Sqrt(b.Sum(kv => kv.Value * kv.Value));
        if (normA == 0 || normB == 0) return 0;

        return Math.Clamp(dot / (normA * normB), 0, 1);
    }

    private sealed class ScoredMovie
    {
        public Movie Movie { get; set; } = null!;
        public double ContentScore { get; set; }
        public double CollabScore { get; set; }
        public double FinalScore { get; set; }
    }

    /// <summary>
    /// Builds a sparse normalized feature vector for a movie.
    /// Features (string keys):
    ///   genre:Action, genre:Drama, ...
    ///   lang:VN, lang:EN, ...
    ///   age:P, age:K, age:T13, age:T16, age:T18
    ///   dur:short (&lt;90m), dur:mid (90-120m), dur:long (&gt;120m)
    ///   decade:2020s, decade:2010s (from ReleaseDate)
    /// </summary>
    private static class MovieVector
    {
        public static Dictionary<string, double> Build(Movie movie)
        {
            var dict = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

            // Genre — could be "Action, Adventure" or single
            var genres = (movie.Genre ?? string.Empty)
                .Split(new[] { ',', ';', '/', '|' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var g in genres)
            {
                if (string.IsNullOrWhiteSpace(g)) continue;
                dict[$"genre:{g.ToLowerInvariant()}"] = 1.0;
            }

            // Language
            var lang = (movie.Language ?? string.Empty).Trim().ToLowerInvariant();
            if (!string.IsNullOrWhiteSpace(lang))
            {
                dict[$"lang:{lang}"] = 1.0;
            }

            // Age rating bucket
            var age = (movie.AgeRating ?? string.Empty).Trim().ToUpperInvariant();
            if (!string.IsNullOrWhiteSpace(age))
            {
                dict[$"age:{age}"] = 1.0;
            }

            // Duration bucket
            var bucket = movie.DurationMin switch
            {
                < 90 => "short",
                <= 120 => "mid",
                _ => "long",
            };
            dict[$"dur:{bucket}"] = 1.0;

            // Decade
            try
            {
                var year = movie.ReleaseDate.Year;
                var decade = (year / 10) * 10;
                dict[$"decade:{decade}"] = 1.0;
            }
            catch
            {
                // ignore DateOnly init issues
            }

            // Normalize
            var norm = Math.Sqrt(dict.Sum(kv => kv.Value * kv.Value));
            if (norm > 0)
            {
                foreach (var k in dict.Keys.ToList())
                {
                    dict[k] /= norm;
                }
            }

            return new Dictionary<string, double>(dict, StringComparer.OrdinalIgnoreCase);
        }
    }
}
