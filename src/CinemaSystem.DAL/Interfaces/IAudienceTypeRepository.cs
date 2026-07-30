using CinemaSystem.DAL.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Interfaces;

public interface IAudienceTypeRepository
{
    IQueryable<AudienceType> Query();

    Task<AudienceType?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<AudienceType>> GetAllActiveAsync(CancellationToken cancellationToken = default);
}
