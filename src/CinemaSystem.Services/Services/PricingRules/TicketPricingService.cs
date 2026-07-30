using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.PricingRules;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.PricingRules;

public sealed class TicketPricingService(
    IShowtimeRepository showtimeRepository,
    ISeatRepository seatRepository,
    IAudienceTypeRepository audienceTypeRepository,
    IPricingRuleRepository pricingRuleRepository,
    ILogger<TicketPricingService> logger) : ITicketPricingService
{
    private readonly ILogger<TicketPricingService> _logger = logger;

    public async Task<TicketPriceResponse> CalculateUnitPriceAsync(
        CalculateTicketPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.SeatIds is null || request.SeatIds.Count == 0)
        {
            throw new InvalidOperationException(PricingRuleMessages.SeatIdsRequired);
        }

        var distinctSeatIds = request.SeatIds.Distinct().ToList();
        if (distinctSeatIds.Count != request.SeatIds.Count)
        {
            throw new InvalidOperationException(PricingRuleMessages.DuplicateSeatIds);
        }

        var showtime = await showtimeRepository.Query()
            .AsNoTracking()
            .Include(item => item.Room)
            .FirstOrDefaultAsync(item => item.Id == request.ShowtimeId, cancellationToken);

        if (showtime is null)
        {
            throw new InvalidOperationException(PricingRuleMessages.ShowtimeNotFound);
        }

        var seats = await seatRepository.Query()
            .AsNoTracking()
            .Include(item => item.SeatType)
            .Where(item => distinctSeatIds.Contains(item.Id))
            .ToListAsync(cancellationToken);

        var foundSeatIds = seats.Select(item => item.Id).ToHashSet();
        var missingSeatIds = distinctSeatIds.Where(id => !foundSeatIds.Contains(id)).ToList();
        if (missingSeatIds.Count > 0)
        {
            _logger.LogWarning(
                "Seat not found while calculating ticket price. ShowtimeId={ShowtimeId}, MissingSeatIds={MissingSeatIds}, RoomId={RoomId}",
                request.ShowtimeId,
                string.Join(",", missingSeatIds),
                showtime.RoomId);
            throw new InvalidOperationException(PricingRuleMessages.SeatNotFound);
        }

        var invalidRoomSeats = seats
            .Where(item => item.RoomId != showtime.RoomId)
            .Select(item => item.Id)
            .ToList();
        if (invalidRoomSeats.Count > 0)
        {
            _logger.LogWarning(
                "Seat(s) do not belong to showtime room. ShowtimeId={ShowtimeId}, InvalidSeatIds={InvalidSeatIds}, ShowtimeRoomId={RoomId}",
                request.ShowtimeId,
                string.Join(",", invalidRoomSeats),
                showtime.RoomId);
            throw new InvalidOperationException(PricingRuleMessages.SeatNotFoundInRoom);
        }

        var audienceType = await audienceTypeRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(
                item => item.Id == request.AudienceTypeId && item.IsActive,
                cancellationToken);

        if (audienceType is null)
        {
            throw new InvalidOperationException(PricingRuleMessages.AudienceTypeNotFound);
        }

        var roomTypeId = request.RoomTypeId
            ?? (int)PricingKindMapper.FromLegacyRoomType(showtime.Room.RoomType);
        var timeSlotId = (int)PricingKindMapper.FromLegacyTimeSlot(showtime.TimeSlot);
        var showDate = DateOnly.FromDateTime(showtime.StartTime);

        _logger.LogInformation(
            "Looking for pricing rule. CinemaId={CinemaId}, RoomTypeId={RoomTypeId}, TimeSlotId={TimeSlotId}, ShowDate={ShowDate}, RoomType={RoomType}, TimeSlot={TimeSlot}",
            showtime.CinemaId,
            roomTypeId,
            timeSlotId,
            showDate,
            showtime.Room.RoomType,
            showtime.TimeSlot);

        var pricingRule = await pricingRuleRepository.Query()
            .AsNoTracking()
            .Where(rule =>
                rule.CinemaId == showtime.CinemaId &&
                rule.RoomTypeId == roomTypeId &&
                rule.TimeSlotId == timeSlotId &&
                rule.IsActive &&
                rule.EffectiveFrom <= showDate &&
                rule.EffectiveTo >= showDate)
            .OrderByDescending(rule => rule.EffectiveFrom)
            .ToListAsync(cancellationToken);

        _logger.LogInformation(
            "Pricing rules query result. CinemaId={CinemaId}, RoomTypeId={RoomTypeId}, TimeSlotId={TimeSlotId}, ShowDate={ShowDate}, TotalRulesFound={TotalRules}",
            showtime.CinemaId,
            roomTypeId,
            timeSlotId,
            showDate,
            pricingRule.Count);

        if (pricingRule.Count > 0)
        {
            _logger.LogInformation(
                "Found rule. Id={RuleId}, BasePrice={BasePrice}, EffectiveFrom={From}, EffectiveTo={To}, TimeMultiplier={Multiplier}",
                pricingRule[0].Id,
                pricingRule[0].BasePrice,
                pricingRule[0].EffectiveFrom,
                pricingRule[0].EffectiveTo,
                pricingRule[0].TimeMultiplier);
        }

        var firstRule = pricingRule.FirstOrDefault();

        if (firstRule is null)
        {
            throw new InvalidOperationException(PricingRuleMessages.NoApplicablePricingRule);
        }

        var audienceMultiplier = audienceType.AudienceMultiplier;
        var timeMultiplier = firstRule.TimeMultiplier;
        var basePrice = firstRule.BasePrice;

        var seatPriceItems = seats
            .OrderBy(item => item.Id)
            .Select(seat =>
            {
                var unitPrice = basePrice * seat.SeatType.SeatMultiplier * audienceMultiplier * timeMultiplier;
                return new SeatPriceItem(seat.Id, unitPrice);
            })
            .ToList();

        var totalPrice = seatPriceItems.Sum(item => item.UnitPrice);

        return new TicketPriceResponse(
            request.ShowtimeId,
            request.AudienceTypeId,
            firstRule.Id,
            basePrice,
            timeMultiplier,
            seatPriceItems,
            totalPrice);
    }
}
