using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Showtimes;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Showtimes;

public sealed class ShowtimeService(
    IMovieRepository movieRepository,
    IRoomRepository roomRepository,
    IShowtimeRepository showtimeRepository) : IShowtimeService
{
    private const string ScheduledStatus = "SCHEDULED";
    private const string ActiveStatus = "ACTIVE";
    private const string CompletedStatus = "COMPLETED";
    private const string CancelledStatus = "CANCELLED";
    private const int GapMinutes = 15;

    public async Task<PagedResult<ShowtimeResponse>> SearchAsync(ShowtimeSearchRequest request, CancellationToken cancellationToken = default)
    {
        IQueryable<Showtime> query = showtimeRepository.Query()
            .AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
            .ThenInclude(room => room.Cinema);

        if (request.MovieId.HasValue)
        {
            query = query.Where(showtime => showtime.MovieId == request.MovieId.Value);
        }

        if (request.CinemaId.HasValue)
        {
            query = query.Where(showtime => showtime.CinemaId == request.CinemaId.Value);
        }

        if (request.RoomId.HasValue)
        {
            query = query.Where(showtime => showtime.RoomId == request.RoomId.Value);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(showtime => showtime.StartTime >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(showtime => showtime.StartTime <= request.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(showtime => showtime.Status == request.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var keyword = request.Search.Trim();
            query = query.Where(showtime =>
                showtime.Movie.Title.Contains(keyword)
                || showtime.Room.Name.Contains(keyword)
                || showtime.Room.Cinema.Name.Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var showtimes = await query
            .OrderBy(showtime => showtime.StartTime)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);
        var items = showtimes.Select(ToResponse).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<ShowtimeResponse>(items, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<ShowtimeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var showtime = await showtimeRepository.Query()
            .AsNoTracking()
            .Include(showtime => showtime.Movie)
            .Include(showtime => showtime.Room)
            .ThenInclude(room => room.Cinema)
            .FirstOrDefaultAsync(showtime => showtime.Id == id, cancellationToken);

        return showtime is null ? null : ToResponse(showtime);
    }

    public async Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        var movie = await movieRepository.GetByIdAsync(request.MovieId, cancellationToken);
        if (movie is null)
        {
            throw new InvalidOperationException(ShowtimeMessages.MovieNotFound);
        }

        var room = await roomRepository.Query()
            .Include(item => item.Cinema)
            .FirstOrDefaultAsync(item => item.Id == request.RoomId, cancellationToken);
        if (room is null)
        {
            throw new InvalidOperationException(ShowtimeMessages.RoomNotFound);
        }

        if (room.CinemaId != room.Cinema.Id)
        {
            throw new InvalidOperationException(RoomMessages.RoomCinemaMappingInvalid);
        }

        var currentTime = DateTime.Now;
        var startTime = request.StartTime;
        if (startTime < currentTime)
        {
            throw new InvalidOperationException(ShowtimeMessages.ShowtimeStartTimeCannotBeInPast);
        }

        var endTime = startTime.AddMinutes(movie.DurationMin);

        if (await showtimeRepository.HasOverlappingShowtimeAsync(room.Id, startTime, endTime, null, cancellationToken))
        {
            throw new BusinessConflictException(ShowtimeMessages.ShowtimeOverlap);
        }

        var gapStart = startTime.AddMinutes(-GapMinutes);
        var gapEnd = endTime.AddMinutes(GapMinutes);
        if (await showtimeRepository.HasOverlappingShowtimeAsync(room.Id, gapStart, gapEnd, null, cancellationToken))
        {
            throw new BusinessConflictException(ShowtimeMessages.ShowtimeGapTooShort);
        }

        var showtime = new Showtime
        {
            Id = Guid.NewGuid(),
            MovieId = movie.Id,
            RoomId = room.Id,
            CinemaId = room.CinemaId,
            CreatedBy = createdByUserId,
            StartTime = startTime,
            EndTime = endTime,
            TimeSlot = request.TimeSlot.Trim(),
            LanguageType = request.LanguageType.Trim(),
            Status = GetStatusByTime(startTime, endTime, currentTime),
            CreatedAt = DateTime.UtcNow
        };

        await showtimeRepository.AddAsync(showtime, cancellationToken);
        await showtimeRepository.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(showtime.Id, cancellationToken) ?? ToResponse(showtime, movie.Title, room.Name, room.Cinema.Name);
    }

    public async Task<ShowtimeResponse?> UpdateAsync(Guid id, UpdateShowtimeRequest request, CancellationToken cancellationToken = default)
    {
        var showtime = await showtimeRepository.Query()
            .Include(item => item.Movie)
            .Include(item => item.Room)
            .ThenInclude(room => room.Cinema)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (showtime is null)
        {
            return null;
        }

        var movie = showtime.Movie;
        if (request.MovieId.HasValue && request.MovieId.Value != showtime.MovieId)
        {
            movie = await movieRepository.GetByIdAsync(request.MovieId.Value, cancellationToken);
            if (movie is null)
            {
                throw new InvalidOperationException(ShowtimeMessages.MovieNotFound);
            }
        }

        var room = showtime.Room;
        if (request.RoomId.HasValue && request.RoomId.Value != showtime.RoomId)
        {
            room = await roomRepository.Query()
                .Include(item => item.Cinema)
                .FirstOrDefaultAsync(item => item.Id == request.RoomId.Value, cancellationToken);
            if (room is null)
            {
                throw new InvalidOperationException(ShowtimeMessages.RoomNotFound);
            }
        }

        if (room.Cinema is null || room.CinemaId != room.Cinema.Id)
        {
            throw new InvalidOperationException(RoomMessages.RoomCinemaMappingInvalid);
        }

        var currentTime = DateTime.Now;
        var startTime = request.StartTime ?? showtime.StartTime;
        var isStartTimeChanged = request.StartTime.HasValue && request.StartTime.Value != showtime.StartTime;
        if (isStartTimeChanged && startTime < currentTime)
        {
            throw new InvalidOperationException(ShowtimeMessages.ShowtimeStartTimeCannotBeInPast);
        }

        var shouldAutoCalculateEndTime = !request.EndTime.HasValue || request.MovieId.HasValue || request.StartTime.HasValue;
        var endTime = shouldAutoCalculateEndTime
            ? startTime.AddMinutes(movie.DurationMin)
            : request.EndTime!.Value;

        if (endTime <= startTime)
        {
            throw new InvalidOperationException(ShowtimeMessages.EndTimeMustBeGreaterThanStartTime);
        }

        if (await showtimeRepository.HasOverlappingShowtimeAsync(room.Id, startTime, endTime, showtime.Id, cancellationToken))
        {
            throw new BusinessConflictException(ShowtimeMessages.ShowtimeOverlap);
        }

        var gapStart = startTime.AddMinutes(-GapMinutes);
        var gapEnd = endTime.AddMinutes(GapMinutes);
        if (await showtimeRepository.HasOverlappingShowtimeAsync(room.Id, gapStart, gapEnd, showtime.Id, cancellationToken))
        {
            throw new BusinessConflictException(ShowtimeMessages.ShowtimeGapTooShort);
        }

        showtime.MovieId = movie.Id;
        showtime.Movie = movie;
        showtime.RoomId = room.Id;
        showtime.Room = room;
        showtime.CinemaId = room.CinemaId;
        showtime.StartTime = startTime;
        showtime.EndTime = endTime;

        if (!string.IsNullOrWhiteSpace(request.TimeSlot))
        {
            showtime.TimeSlot = request.TimeSlot.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.LanguageType))
        {
            showtime.LanguageType = request.LanguageType.Trim();
        }

        if (IsCancelledStatus(request.Status) || IsCancelledStatus(showtime.Status))
        {
            showtime.Status = CancelledStatus;
        }
        else
        {
            showtime.Status = GetStatusByTime(startTime, endTime, currentTime);
        }

        showtimeRepository.Update(showtime);
        await showtimeRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(showtime);
    }

    public async Task<DeleteShowtimeResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var showtime = await showtimeRepository.Query()
            .Include(item => item.BookingSeats)
            .Include(item => item.Movie)
            .Include(item => item.Room)
            .ThenInclude(room => room.Cinema)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (showtime is null)
        {
            return DeleteShowtimeResult.NotFound;
        }

        if (showtime.BookingSeats.Count > 0)
        {
            showtime.Status = CancelledStatus;
            showtimeRepository.Update(showtime);
            await showtimeRepository.SaveChangesAsync(cancellationToken);
            return DeleteShowtimeResult.Cancelled;
        }

        showtimeRepository.Delete(showtime);
        await showtimeRepository.SaveChangesAsync(cancellationToken);
        return DeleteShowtimeResult.Deleted;
    }

    public async Task<int> SyncShowtimeStatusesAsync(DateTime currentTime, CancellationToken cancellationToken = default)
    {
        var showtimes = await showtimeRepository.Query()
            .Where(showtime => showtime.Status != CancelledStatus)
            .ToListAsync(cancellationToken);

        if (showtimes.Count == 0)
        {
            return 0;
        }

        var changedCount = 0;
        foreach (var showtime in showtimes)
        {
            var nextStatus = GetStatusByTime(showtime.StartTime, showtime.EndTime, currentTime);
            if (string.Equals(showtime.Status, nextStatus, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            showtime.Status = nextStatus;
            showtimeRepository.Update(showtime);
            changedCount++;
        }

        if (changedCount == 0)
        {
            return 0;
        }

        await showtimeRepository.SaveChangesAsync(cancellationToken);
        return changedCount;
    }

    private static string GetStatusByTime(DateTime startTime, DateTime endTime, DateTime currentTime)
    {
        if (currentTime < startTime)
        {
            return ScheduledStatus;
        }

        return currentTime < endTime ? ActiveStatus : CompletedStatus;
    }

    private static bool IsCancelledStatus(string? status)
        => string.Equals(status?.Trim(), CancelledStatus, StringComparison.OrdinalIgnoreCase);

    private static ShowtimeResponse ToResponse(Showtime showtime)
        => new(
            showtime.Id,
            showtime.MovieId,
            showtime.Movie.Title,
            showtime.RoomId,
            showtime.Room.Name,
            showtime.CinemaId,
            showtime.Room.Cinema.Name,
            showtime.StartTime,
            showtime.EndTime,
            showtime.TimeSlot,
            showtime.LanguageType,
            showtime.Status);

    private static ShowtimeResponse ToResponse(Showtime showtime, string movieTitle, string roomName, string cinemaName)
        => new(
            showtime.Id,
            showtime.MovieId,
            movieTitle,
            showtime.RoomId,
            roomName,
            showtime.CinemaId,
            cinemaName,
            showtime.StartTime,
            showtime.EndTime,
            showtime.TimeSlot,
            showtime.LanguageType,
            showtime.Status);
}
