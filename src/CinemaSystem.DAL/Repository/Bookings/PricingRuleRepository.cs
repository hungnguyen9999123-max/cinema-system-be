using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repository.Bookings;

internal sealed class BookingPricingRuleRepository
{
    private readonly CinemaDbContext _dbContext;

    public BookingPricingRuleRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PricingRule?> GetActivePricingRuleAsync(Guid cinemaId, string roomType, string timeSlot, DateTime date, CancellationToken cancellationToken = default)
    {
        var dateOnly = DateOnly.FromDateTime(date);
        var roomTypeId = (int)PricingKindMapper.FromLegacyRoomType(roomType);
        var timeSlotId = (int)PricingKindMapper.FromLegacyTimeSlot(timeSlot);

        return await _dbContext.PricingRules
            .Where(p => p.CinemaId == cinemaId
                     && p.RoomTypeId == roomTypeId
                     && p.TimeSlotId == timeSlotId
                     && p.IsActive
                     && p.EffectiveFrom <= dateOnly
                     && p.EffectiveTo >= dateOnly)
            .OrderByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
