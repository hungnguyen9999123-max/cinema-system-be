using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Rooms;

public sealed class RoomService(
    ICinemaRepository cinemaRepository,
    IRoomRepository roomRepository,
    ISeatRepository seatRepository) : IRoomService
{
    public async Task<PagedResult<RoomResponse>> SearchAsync(RoomSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = roomRepository.Query()
            .AsNoTracking()
            .Include(room => room.Cinema)
            .Include(room => room.Seats)
            .AsQueryable();

        if (request.CinemaId.HasValue)
        {
            query = query.Where(room => room.CinemaId == request.CinemaId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.RoomType))
        {
            query = query.Where(room => room.RoomType == request.RoomType.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(room => room.Status == request.Status.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(room =>
                room.Name.Contains(keyword) ||
                room.Cinema.Name.Contains(keyword) ||
                room.Cinema.City.Contains(keyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(room => room.Cinema.Name)
            .ThenBy(room => room.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(room => new RoomResponse(
                room.Id,
                room.CinemaId,
                room.Cinema.Name,
                room.Name,
                room.RoomType,
                room.TotalCapacity,
                room.Status,
                room.Seats.Count(seat => seat.Status == "ACTIVE"),
                room.CreatedAt))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<RoomResponse>(items, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<RoomResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .AsNoTracking()
            .Include(item => item.Cinema)
            .Include(item => item.Seats)
            .Where(item => item.Id == id)
            .Select(item => new RoomResponse(
                item.Id,
                item.CinemaId,
                item.Cinema.Name,
                item.Name,
                item.RoomType,
                item.TotalCapacity,
                item.Status,
                item.Seats.Count(seat => seat.Status == "ACTIVE"),
                item.CreatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return room;
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        if (!await cinemaRepository.ExistsAsync(request.CinemaId, cancellationToken))
        {
            throw new InvalidOperationException(RoomMessages.CinemaNotFound);
        }

        var roomName = request.Name.Trim();
        if (await roomRepository.Query().AnyAsync(room => room.CinemaId == request.CinemaId && room.Name == roomName, cancellationToken))
        {
            throw new BusinessConflictException(RoomMessages.RoomNameAlreadyExists);
        }

        var room = new Room
        {
            Id = Guid.NewGuid(),
            CinemaId = request.CinemaId,
            Name = roomName,
            RoomType = request.RoomType.Trim(),
            TotalCapacity = request.TotalCapacity,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow
        };

        await roomRepository.AddAsync(room, cancellationToken);
        await roomRepository.SaveChangesAsync(cancellationToken);

        var cinema = await cinemaRepository.GetByIdAsync(room.CinemaId, cancellationToken);
        return new RoomResponse(room.Id, room.CinemaId, cinema?.Name ?? string.Empty, room.Name, room.RoomType, room.TotalCapacity, room.Status, 0, room.CreatedAt);
    }

    public async Task<RoomResponse?> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .Include(item => item.Cinema)
            .Include(item => item.Seats)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (room is null)
        {
            return null;
        }

        var roomName = request.Name.Trim();
        if (await roomRepository.Query().AnyAsync(item => item.CinemaId == room.CinemaId && item.Name == roomName && item.Id != id, cancellationToken))
        {
            throw new BusinessConflictException(RoomMessages.RoomNameAlreadyExists);
        }

        room.Name = roomName;
        room.RoomType = request.RoomType.Trim();
        room.TotalCapacity = request.TotalCapacity;
        room.Status = request.Status.Trim();

        roomRepository.Update(room);
        await roomRepository.SaveChangesAsync(cancellationToken);

        return new RoomResponse(
            room.Id,
            room.CinemaId,
            room.Cinema.Name,
            room.Name,
            room.RoomType,
            room.TotalCapacity,
            room.Status,
            room.Seats.Count(seat => seat.Status == "ACTIVE"),
            room.CreatedAt);
    }

    public async Task<DeleteRoomResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .Include(item => item.Seats)
            .ThenInclude(seat => seat.BookingSeats)
            .Include(item => item.Showtimes)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (room is null)
        {
            return DeleteRoomResult.NotFound;
        }

        if (room.Showtimes.Any(showtime => showtime.Status != "CANCELLED" && showtime.Status != "COMPLETED"))
        {
            throw new BusinessConflictException(RoomMessages.RoomCannotBeDeletedBecauseHasActiveShowtimes);
        }

        var lockedSeats = room.Seats
            .Where(seat => seat.BookingSeats.Count > 0)
            .Select(seat => seat.SeatLabel)
            .ToList();

        if (lockedSeats.Count > 0)
        {
            throw new BusinessConflictException($"{RoomMessages.RoomCannotBeDeletedBecauseSeatsHaveBookingHistory} {string.Join(", ", lockedSeats)}");
        }

        if (room.Seats.Count > 0)
        {
            seatRepository.DeleteRange(room.Seats);
        }

        roomRepository.Delete(room);
        await roomRepository.SaveChangesAsync(cancellationToken);
        return DeleteRoomResult.Deleted;
    }
}
