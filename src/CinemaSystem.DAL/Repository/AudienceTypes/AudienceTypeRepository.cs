using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repository.AudienceTypes;

public sealed class AudienceTypeRepository(CinemaDbContext dbContext) : IAudienceTypeRepository
{
    public IQueryable<AudienceType> Query() => dbContext.AudienceTypes.AsQueryable();

    public async Task<AudienceType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.AudienceTypes.FindAsync([id], cancellationToken);

    public async Task<IEnumerable<AudienceType>> GetAllActiveAsync(CancellationToken cancellationToken = default)
        => await dbContext.AudienceTypes
            .Where(a => a.IsActive)
            .OrderBy(a => a.DisplayName)
            .ToListAsync(cancellationToken);
}
