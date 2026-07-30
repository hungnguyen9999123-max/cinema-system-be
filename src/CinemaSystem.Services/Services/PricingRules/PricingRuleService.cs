using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.PricingRules;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.PricingRules;

public sealed class PricingRuleService(
    ICinemaRepository cinemaRepository,
    IPricingRuleRepository pricingRuleRepository) : IPricingRuleService
{
    private static readonly int[] DefaultRoomTypeIds =
    [
        (int)RoomTypeKind.Standard,
        (int)RoomTypeKind.Vip,
        (int)RoomTypeKind.Imax,
        (int)RoomTypeKind.FourDx
    ];

    private static readonly int[] DefaultTimeSlotIds =
    [
        (int)TimeSlotKind.Normal,
        (int)TimeSlotKind.Peak,
        (int)TimeSlotKind.Evening,
        (int)TimeSlotKind.Midnight
    ];

    public async Task GenerateDefaultPricingRulesAsync(
        Guid cinemaId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCinemaExistsAsync(cinemaId, cancellationToken);

        var hasExistingActiveRules = await pricingRuleRepository.Query()
            .AnyAsync(rule => rule.CinemaId == cinemaId && rule.IsActive, cancellationToken);

        if (hasExistingActiveRules)
        {
            throw new BusinessConflictException(PricingRuleMessages.DefaultRulesAlreadyExist);
        }

        await PersistDefaultRulesAsync(cinemaId, cancellationToken);
    }

    public async Task RegenerateDefaultPricingRulesAsync(
        Guid cinemaId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCinemaExistsAsync(cinemaId, cancellationToken);

        var activeRules = await pricingRuleRepository.Query()
            .Where(rule => rule.CinemaId == cinemaId && rule.IsActive)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        foreach (var rule in activeRules)
        {
            rule.IsActive = false;
            rule.EffectiveTo = DateOnly.FromDateTime(now);
            pricingRuleRepository.Update(rule);
        }

        await pricingRuleRepository.SaveChangesAsync(cancellationToken);

        await PersistDefaultRulesAsync(cinemaId, cancellationToken);
    }

    public async Task<IReadOnlyList<PricingRuleResponse>> GetByCinemaIdAsync(
        Guid cinemaId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCinemaExistsAsync(cinemaId, cancellationToken);

        return await pricingRuleRepository.Query()
            .AsNoTracking()
            .Include(rule => rule.Cinema)
            .Where(rule => rule.CinemaId == cinemaId)
            .OrderBy(rule => rule.RoomTypeId)
            .ThenBy(rule => rule.TimeSlotId)
            .Select(rule => ToResponse(rule))
            .ToListAsync(cancellationToken);
    }

    public async Task<PricingRuleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await pricingRuleRepository.Query()
            .AsNoTracking()
            .Include(rule => rule.Cinema)
            .Where(rule => rule.Id == id)
            .Select(rule => ToResponse(rule))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<PricingRuleResponse?> UpdateAsync(
        Guid id,
        UpdatePricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRulePayload(request.BasePrice, request.TimeMultiplier);

        var rule = await pricingRuleRepository.Query()
            .Include(item => item.Cinema)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (rule is null)
        {
            return null;
        }

        rule.BasePrice = request.BasePrice;
        rule.TimeMultiplier = request.TimeMultiplier;
        rule.IsActive = request.IsActive;

        pricingRuleRepository.Update(rule);
        await pricingRuleRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(rule);
    }

    public async Task<PricingRuleResponse> CreateAsync(
        Guid cinemaId,
        CreatePricingRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCinemaExistsAsync(cinemaId, cancellationToken);

        if (!PricingKindMapper.IsValidRoomTypeId(request.RoomTypeId))
        {
            throw new InvalidOperationException(PricingRuleMessages.InvalidRoomTypeId);
        }

        if (!PricingKindMapper.IsValidTimeSlotId(request.TimeSlotId))
        {
            throw new InvalidOperationException(PricingRuleMessages.InvalidTimeSlotId);
        }

        ValidateRulePayload(request.BasePrice, request.TimeMultiplier);

        if (request.EffectiveFrom > request.EffectiveTo)
        {
            throw new InvalidOperationException(PricingRuleMessages.InvalidEffectiveDateRange);
        }

        var overlap = await pricingRuleRepository.Query()
            .AnyAsync(rule =>
                rule.CinemaId == cinemaId &&
                rule.RoomTypeId == request.RoomTypeId &&
                rule.TimeSlotId == request.TimeSlotId &&
                rule.IsActive &&
                rule.EffectiveFrom <= request.EffectiveTo &&
                rule.EffectiveTo >= request.EffectiveFrom,
                cancellationToken);

        if (overlap)
        {
            throw new BusinessConflictException(PricingRuleMessages.ActiveRuleAlreadyExists);
        }

        var rule = new PricingRule
        {
            Id = Guid.NewGuid(),
            CinemaId = cinemaId,
            RoomTypeId = request.RoomTypeId,
            TimeSlotId = request.TimeSlotId,
            BasePrice = request.BasePrice,
            TimeMultiplier = request.TimeMultiplier,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await pricingRuleRepository.AddAsync(rule, cancellationToken);
        await pricingRuleRepository.SaveChangesAsync(cancellationToken);

        var withCinema = await pricingRuleRepository.Query()
            .AsNoTracking()
            .Include(item => item.Cinema)
            .FirstAsync(item => item.Id == rule.Id, cancellationToken);

        return ToResponse(withCinema);
    }

    private async Task EnsureCinemaExistsAsync(Guid cinemaId, CancellationToken cancellationToken)
    {
        if (!await cinemaRepository.ExistsAsync(cinemaId, cancellationToken))
        {
            throw new InvalidOperationException(PricingRuleMessages.CinemaNotFound);
        }
    }

    private async Task PersistDefaultRulesAsync(Guid cinemaId, CancellationToken cancellationToken)
    {
        var effectiveFrom = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;

        foreach (var roomTypeId in DefaultRoomTypeIds)
        {
            foreach (var timeSlotId in DefaultTimeSlotIds)
            {
                var rule = new PricingRule
                {
                    Id = Guid.NewGuid(),
                    CinemaId = cinemaId,
                    RoomTypeId = roomTypeId,
                    TimeSlotId = timeSlotId,
                    BasePrice = GetDefaultBasePrice(roomTypeId),
                    TimeMultiplier = GetDefaultTimeMultiplier(timeSlotId),
                    EffectiveFrom = effectiveFrom,
                    EffectiveTo = PricingRuleDefaults.DefaultEffectiveTo,
                    IsActive = true,
                    CreatedAt = now
                };

                await pricingRuleRepository.AddAsync(rule, cancellationToken);
            }
        }

        await pricingRuleRepository.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateRulePayload(decimal basePrice, decimal timeMultiplier)
    {
        if (basePrice <= 0)
        {
            throw new InvalidOperationException(PricingRuleMessages.InvalidBasePrice);
        }

        if (timeMultiplier <= 0)
        {
            throw new InvalidOperationException(PricingRuleMessages.InvalidTimeMultiplier);
        }
    }

    private static decimal GetDefaultBasePrice(int roomTypeId)
        => roomTypeId switch
        {
            (int)RoomTypeKind.Standard => PricingRuleDefaults.StandardBasePrice,
            (int)RoomTypeKind.Vip => PricingRuleDefaults.VipBasePrice,
            (int)RoomTypeKind.Imax => PricingRuleDefaults.ImaxBasePrice,
            (int)RoomTypeKind.FourDx => PricingRuleDefaults.FourDxBasePrice,
            _ => PricingRuleDefaults.StandardBasePrice
        };

    private static decimal GetDefaultTimeMultiplier(int timeSlotId)
        => timeSlotId switch
        {
            (int)TimeSlotKind.Normal => PricingRuleDefaults.NormalTimeMultiplier,
            (int)TimeSlotKind.Peak => PricingRuleDefaults.PeakTimeMultiplier,
            (int)TimeSlotKind.Evening => PricingRuleDefaults.EveningTimeMultiplier,
            (int)TimeSlotKind.Midnight => PricingRuleDefaults.MidnightTimeMultiplier,
            _ => PricingRuleDefaults.NormalTimeMultiplier
        };

    private static PricingRuleResponse ToResponse(PricingRule rule)
        => new(
            rule.Id,
            rule.CinemaId,
            rule.Cinema.Name,
            rule.RoomTypeId,
            rule.TimeSlotId,
            rule.BasePrice,
            rule.TimeMultiplier,
            rule.EffectiveFrom,
            rule.EffectiveTo,
            rule.IsActive,
            rule.CreatedAt);
}
