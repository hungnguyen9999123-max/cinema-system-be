using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Cinemas;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.PricingRules;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Cinemas;

public sealed class CinemaService(
    ICinemaRepository cinemaRepository,
    IPricingRuleService pricingRuleService,
    IUnitOfWork unitOfWork) : ICinemaService
{
    private static readonly string[] EditableStatuses = ["ACTIVE", "INACTIVE"];
    private const string InactiveStatus = "INACTIVE";

    public async Task<PagedResult<CinemaResponse>> SearchAsync(CinemaSearchRequest request, CancellationToken cancellationToken = default)
    {
        var query = cinemaRepository.Query()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(cinema =>
                cinema.Name.Contains(keyword) ||
                cinema.Address.Contains(keyword) ||
                cinema.City.Contains(keyword));
        }

        if (!string.IsNullOrWhiteSpace(request.City))
        {
            var city = request.City.Trim();
            query = query.Where(cinema => cinema.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            if (!IsEditableStatus(status))
            {
                throw new InvalidOperationException(CinemaMessages.InvalidStatus);
            }

            query = query.Where(cinema => cinema.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(cinema => cinema.City)
            .ThenBy(cinema => cinema.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(cinema => ToResponse(cinema))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<CinemaResponse>(items, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<CinemaResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await cinemaRepository.Query()
            .AsNoTracking()
            .Where(cinema => cinema.Id == id)
            .Select(cinema => ToResponse(cinema))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<CinemaResponse> CreateAsync(CreateCinemaRequest request, CancellationToken cancellationToken = default)
    {
        var status = NormalizeStatus(request.Status);
        if (!IsEditableStatus(status))
        {
            throw new InvalidOperationException(CinemaMessages.InvalidStatus);
        }

        var name = request.Name.Trim();
        if (await IsNameExistsAsync(name, null, cancellationToken))
        {
            throw new BusinessConflictException(CinemaMessages.NameAlreadyExists);
        }

        var now = DateTime.UtcNow;
        var cinema = new Cinema
        {
            Id = Guid.NewGuid(),
            Name = name,
            Address = request.Address.Trim(),
            City = request.City.Trim(),
            Phone = NormalizeOptional(request.Phone),
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await cinemaRepository.AddAsync(cinema, cancellationToken);

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await pricingRuleService.GenerateDefaultPricingRulesAsync(cinema.Id, cancellationToken);
            await cinemaRepository.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        return ToResponse(cinema);
    }

    public async Task<CinemaResponse?> UpdateAsync(Guid id, UpdateCinemaRequest request, CancellationToken cancellationToken = default)
    {
        var cinema = await cinemaRepository.Query()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (cinema is null)
        {
            return null;
        }

        var status = NormalizeStatus(request.Status);
        if (!IsEditableStatus(status))
        {
            throw new InvalidOperationException(CinemaMessages.InvalidStatus);
        }

        var name = request.Name.Trim();
        if (await IsNameExistsAsync(name, id, cancellationToken))
        {
            throw new BusinessConflictException(CinemaMessages.NameAlreadyExists);
        }

        cinema.Name = name;
        cinema.Address = request.Address.Trim();
        cinema.City = request.City.Trim();
        cinema.Phone = NormalizeOptional(request.Phone);
        cinema.Status = status;
        cinema.UpdatedAt = DateTime.UtcNow;

        cinemaRepository.Update(cinema);
        await cinemaRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(cinema);
    }

    public async Task<DeleteCinemaResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cinema = await cinemaRepository.Query()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (cinema is null)
        {
            return DeleteCinemaResult.NotFound;
        }

        cinema.Status = InactiveStatus;
        cinema.UpdatedAt = DateTime.UtcNow;

        cinemaRepository.Update(cinema);
        await cinemaRepository.SaveChangesAsync(cancellationToken);

        return DeleteCinemaResult.Deleted;
    }

    private async Task<bool> IsNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        return await cinemaRepository.Query()
            .AnyAsync(cinema =>
                cinema.Name == name &&
                (!excludedId.HasValue || cinema.Id != excludedId.Value),
                cancellationToken);
    }

    private static CinemaResponse ToResponse(Cinema cinema)
        => new(
            cinema.Id,
            cinema.Name,
            cinema.Address,
            cinema.City,
            cinema.Phone,
            cinema.Status,
            cinema.CreatedAt,
            cinema.UpdatedAt);

    private static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsEditableStatus(string status)
        => EditableStatuses.Contains(status);
}
