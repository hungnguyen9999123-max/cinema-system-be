using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Rooms;

public sealed class SeatService(
    IRoomRepository roomRepository,
    ISeatRepository seatRepository,
    ISeatTypeRepository seatTypeRepository) : ISeatService
{
    public async Task<SeatLayoutResponse?> GetLayoutAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .AsNoTracking()
            .Include(item => item.Seats)
            .ThenInclude(seat => seat.SeatType)
            .FirstOrDefaultAsync(item => item.Id == roomId, cancellationToken);

        if (room is null)
        {
            return null;
        }

        var rows = room.Seats
            .OrderBy(seat => seat.RowLetter)
            .ThenBy(seat => seat.ColNumber)
            .GroupBy(seat => seat.RowLetter)
            .Select(group => new SeatRowResponse(
                group.Key,
                group.OrderBy(seat => seat.ColNumber)
                    .Select(seat => ToResponse(seat, seat.SeatType))
                    .ToList()))
            .ToList();

        return new SeatLayoutResponse(room.Id, room.Name, room.Seats.Count(seat => seat.Status == "ACTIVE"), rows);
    }

    public async Task<SeatLayoutResponse> GenerateLayoutAsync(Guid roomId, GenerateSeatLayoutRequest request, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .Include(item => item.Seats)
            .ThenInclude(seat => seat.BookingSeats)
            .ThenInclude(bookingSeat => bookingSeat.Booking)
            .Include(item => item.Cinema)
            .FirstOrDefaultAsync(item => item.Id == roomId, cancellationToken);

        if (room is null)
        {
            throw new InvalidOperationException(RoomMessages.RoomNotFound);
        }

        var defaultSeatType = await ResolveSeatTypeAsync(request.DefaultSeatTypeName, cancellationToken);
        var overrides = request.Overrides ?? [];

        if (!request.ReplaceExisting && room.Seats.Count > 0)
        {
            throw new BusinessConflictException(RoomMessages.RoomAlreadyHasSeats);
        }

        var bookedSeats = room.Seats
            .Where(seat => seat.BookingSeats.Count > 0)
            .Select(seat => seat.SeatLabel)
            .ToList();

        if (request.ReplaceExisting && bookedSeats.Count > 0)
        {
            throw new BusinessConflictException($"{RoomMessages.SeatLabelsAlreadyHaveBookingHistory} {string.Join(", ", bookedSeats)}");
        }

        if (request.ReplaceExisting && room.Seats.Count > 0)
        {
            seatRepository.DeleteRange(room.Seats);
        }

        var seats = new List<Seat>();
        for (var rowIndex = 0; rowIndex < request.Rows; rowIndex++)
        {
            var rowLetter = ((char)('A' + rowIndex)).ToString();
            for (var col = 1; col <= request.SeatsPerRow; col++)
            {
                var overrideRule = overrides.LastOrDefault(rule => IsInOverride(rule, rowLetter, col));
                var seatType = overrideRule is null
                    ? defaultSeatType
                    : await ResolveSeatTypeAsync(overrideRule.SeatTypeName, cancellationToken);

                var status = overrideRule?.Status?.Trim() ?? "ACTIVE";
                seats.Add(new Seat
                {
                    Id = Guid.NewGuid(),
                    RoomId = room.Id,
                    SeatTypeId = seatType.Id,
                    RowLetter = rowLetter,
                    ColNumber = (byte)col,
                    SeatLabel = $"{rowLetter}{col}",
                    Status = status
                });
            }
        }

        await seatRepository.AddRangeAsync(seats, cancellationToken);

        room.TotalCapacity = seats.Count(seat => seat.Status == "ACTIVE");
        roomRepository.Update(room);
        await roomRepository.SaveChangesAsync(cancellationToken);

        return await GetLayoutAsync(roomId, cancellationToken) ?? new SeatLayoutResponse(roomId, room.Name, room.TotalCapacity, []);
    }

    public async Task<SeatResponse> CreateSeatAsync(Guid roomId, CreateSeatRequest request, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.Query()
            .Include(item => item.Seats)
            .FirstOrDefaultAsync(item => item.Id == roomId, cancellationToken);

        if (room is null)
        {
            throw new InvalidOperationException(RoomMessages.RoomNotFound);
        }

        var seatLabel = NormalizeSeatLabel(request.RowLetter, request.ColNumber);
        if (await seatRepository.ExistsLabelInRoomAsync(roomId, seatLabel, null, cancellationToken))
        {
            throw new BusinessConflictException(RoomMessages.SeatLabelAlreadyExists);
        }

        var seatType = await ResolveSeatTypeAsync(request.SeatTypeName, cancellationToken);
        var seat = new Seat
        {
            Id = Guid.NewGuid(),
            RoomId = roomId,
            SeatTypeId = seatType.Id,
            RowLetter = request.RowLetter.Trim().ToUpperInvariant(),
            ColNumber = request.ColNumber,
            SeatLabel = seatLabel,
            Status = "ACTIVE"
        };

        await seatRepository.AddAsync(seat, cancellationToken);
        room.TotalCapacity = room.Seats.Count(item => item.Status == "ACTIVE") + 1;
        roomRepository.Update(room);
        await roomRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(seat, seatType);
    }

    public async Task<SeatResponse?> UpdateSeatAsync(Guid seatId, UpdateSeatRequest request, CancellationToken cancellationToken = default)
    {
        var seat = await seatRepository.Query()
            .Include(item => item.Room)
            .ThenInclude(room => room.Seats)
            .Include(item => item.SeatType)
            .FirstOrDefaultAsync(item => item.Id == seatId, cancellationToken);

        if (seat is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.SeatTypeName))
        {
            seat.SeatType = await ResolveSeatTypeAsync(request.SeatTypeName, cancellationToken);
            seat.SeatTypeId = seat.SeatType.Id;
        }

        seat.Status = request.Status.Trim();
        seatRepository.Update(seat);

        seat.Room.TotalCapacity = seat.Room.Seats.Count(item => item.Status == "ACTIVE");
        roomRepository.Update(seat.Room);

        await seatRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(seat, seat.SeatType);
    }

    public async Task<DeleteSeatResult> DeleteSeatAsync(Guid seatId, CancellationToken cancellationToken = default)
    {
        var seat = await seatRepository.Query()
            .Include(item => item.Room)
            .ThenInclude(room => room.Seats)
            .Include(item => item.SeatType)
            .Include(item => item.BookingSeats)
            .FirstOrDefaultAsync(item => item.Id == seatId, cancellationToken);

        if (seat is null)
        {
            return DeleteSeatResult.NotFound;
        }

        if (seat.BookingSeats.Count > 0)
        {
            seat.Status = "DISABLED";
            seatRepository.Update(seat);
            seat.Room.TotalCapacity = seat.Room.Seats.Count(item => item.Status == "ACTIVE" && item.Id != seat.Id);
            roomRepository.Update(seat.Room);
            await seatRepository.SaveChangesAsync(cancellationToken);
            return DeleteSeatResult.Disabled;
        }

        seatRepository.Delete(seat);
        seat.Room.TotalCapacity = seat.Room.Seats.Count(item => item.Status == "ACTIVE" && item.Id != seat.Id);
        roomRepository.Update(seat.Room);
        await seatRepository.SaveChangesAsync(cancellationToken);
        return DeleteSeatResult.Deleted;
    }

    private async Task<SeatType> ResolveSeatTypeAsync(string seatTypeName, CancellationToken cancellationToken)
    {
        var seatType = await seatTypeRepository.GetByNameAsync(seatTypeName.Trim(), cancellationToken);
        if (seatType is null || !string.Equals(seatType.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(RoomMessages.SeatTypeNotAvailable);
        }

        return seatType;
    }

    private static bool IsInOverride(SeatRangeOverride rule, string rowLetter, int col)
    {
        var rowFrom = rule.RowFrom.Trim().ToUpperInvariant()[0];
        var rowTo = rule.RowTo.Trim().ToUpperInvariant()[0];
        var currentRow = rowLetter[0];
        return currentRow >= rowFrom && currentRow <= rowTo && col >= rule.ColFrom && col <= rule.ColTo;
    }

    private static string NormalizeSeatLabel(string rowLetter, byte colNumber)
        => $"{rowLetter.Trim().ToUpperInvariant()}{colNumber}";

    private static SeatResponse ToResponse(Seat seat, SeatType seatType)
        => new(
            seat.Id,
            seat.RoomId,
            seat.SeatLabel,
            seat.RowLetter,
            seat.ColNumber,
            seatType.Name,
            seatType.SeatMultiplier,
            seat.Status);
}
