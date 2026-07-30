using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repository.Bookings;

internal sealed class BookingAudienceTypeRepository
{
    private readonly CinemaDbContext _dbContext;

    public BookingAudienceTypeRepository(CinemaDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AudienceType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.AudienceTypes.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }
}
