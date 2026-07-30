using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Uploads;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Fnb;

public sealed class FnbItemService(
    IFnbItemRepository fnbItemRepository,
    ICloudinaryService cloudinaryService) : IFnbItemService
{
    private static readonly string[] ValidTypes = ["COMBO", "FOOD", "DRINK"];
    private static readonly string[] EditableStatuses = ["ACTIVE", "INACTIVE"];
    private const string ActiveStatus = "ACTIVE";
    private const string InactiveStatus = "INACTIVE";

    public async Task<PagedResult<FnbItemResponse>> SearchAsync(
        FnbItemSearchRequest request,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = fnbItemRepository.Query().AsNoTracking();

        if (activeOnly)
        {
            query = query.Where(item => item.Status == ActiveStatus);
        }
        else if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            if (!IsEditableStatus(status))
            {
                throw new InvalidOperationException(FnbMessages.InvalidStatus);
            }

            query = query.Where(item => item.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            query = query.Where(item =>
                item.Name.Contains(keyword) ||
                (item.Description != null && item.Description.Contains(keyword)));
        }

        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            var type = NormalizeType(request.Type);
            if (!IsValidType(type))
            {
                throw new InvalidOperationException(FnbMessages.InvalidType);
            }

            query = query.Where(item => item.Category == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(item => item.Category)
            .ThenBy(item => item.Name)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => ToResponse(item))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<FnbItemResponse>(items, request.Page, request.PageSize, totalCount, totalPages);
    }

    public async Task<FnbItemResponse?> GetByIdAsync(
        Guid id,
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = fnbItemRepository.Query()
            .AsNoTracking()
            .Where(item => item.Id == id);

        if (activeOnly)
        {
            query = query.Where(item => item.Status == ActiveStatus);
        }

        return await query
            .Select(item => ToResponse(item))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<FnbItemResponse> CreateAsync(
        CreateFnbItemRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();
        var type = NormalizeType(request.Type);
        var status = NormalizeStatus(request.Status);

        ValidateBusinessRules(type, status, request.Price);

        if (await IsNameExistsAsync(name, null, cancellationToken))
        {
            throw new BusinessConflictException(FnbMessages.NameAlreadyExists);
        }

        var now = DateTime.UtcNow;
        var item = new FnbItem
        {
            Id = Guid.NewGuid(),
            CreatedBy = createdBy,
            Name = name,
            Category = type,
            Description = NormalizeOptional(request.Description),
            Price = request.Price,
            ImageUrl = NormalizeOptional(request.ImageUrl),
            ImagePublicId = NormalizeOptional(request.ImagePublicId),
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await fnbItemRepository.AddAsync(item, cancellationToken);
        await fnbItemRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
    }

    public async Task<FnbItemResponse?> UpdateAsync(
        Guid id,
        UpdateFnbItemRequest request,
        CancellationToken cancellationToken = default)
    {
        var item = await fnbItemRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        var name = request.Name.Trim();
        var type = NormalizeType(request.Type);
        var status = NormalizeStatus(request.Status);

        ValidateBusinessRules(type, status, request.Price);

        if (await IsNameExistsAsync(name, id, cancellationToken))
        {
            throw new BusinessConflictException(FnbMessages.NameAlreadyExists);
        }

        await DeleteReplacedImageAsync(
            item.ImagePublicId,
            request.ImagePublicId,
            cancellationToken);

        item.Name = name;
        item.Category = type;
        item.Description = NormalizeOptional(request.Description);
        item.Price = request.Price;
        item.ImageUrl = NormalizeOptional(request.ImageUrl);
        item.ImagePublicId = NormalizeOptional(request.ImagePublicId);
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;

        fnbItemRepository.Update(item);
        await fnbItemRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(item);
    }

    public async Task<DeleteFnbItemResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var item = await fnbItemRepository.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return DeleteFnbItemResult.NotFound;
        }

        if (!string.IsNullOrWhiteSpace(item.ImagePublicId))
        {
            await cloudinaryService.DeleteImageAsync(
                item.ImagePublicId,
                cancellationToken);
        }

        item.Status = InactiveStatus;
        item.ImageUrl = null;
        item.ImagePublicId = null;
        item.UpdatedAt = DateTime.UtcNow;

        fnbItemRepository.Update(item);
        await fnbItemRepository.SaveChangesAsync(cancellationToken);

        return DeleteFnbItemResult.Deleted;
    }

    private async Task<bool> IsNameExistsAsync(string name, Guid? excludedId, CancellationToken cancellationToken)
    {
        return await fnbItemRepository.Query()
            .AnyAsync(item =>
                item.Name == name &&
                (!excludedId.HasValue || item.Id != excludedId.Value),
                cancellationToken);
    }

    private static void ValidateBusinessRules(string type, string status, decimal price)
    {
        if (!IsValidType(type))
        {
            throw new InvalidOperationException(FnbMessages.InvalidType);
        }

        if (price <= 0)
        {
            throw new InvalidOperationException(FnbMessages.InvalidPrice);
        }

        if (!IsEditableStatus(status))
        {
            throw new InvalidOperationException(FnbMessages.InvalidStatus);
        }
    }

    private static FnbItemResponse ToResponse(FnbItem item)
        => new(
            item.Id,
            item.CreatedBy,
            item.Name,
            item.Category,
            item.Description,
            item.Price,
            item.ImageUrl,
            item.ImagePublicId,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt);

    private static string NormalizeType(string type) => type.Trim().ToUpperInvariant();

    private static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool IsValidType(string type) => ValidTypes.Contains(type);

    private static bool IsEditableStatus(string status) => EditableStatuses.Contains(status);

    private async Task DeleteReplacedImageAsync(
        string? currentPublicId,
        string? newPublicId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(currentPublicId) &&
            !string.Equals(
                currentPublicId,
                NormalizeOptional(newPublicId),
                StringComparison.Ordinal))
        {
            await cloudinaryService.DeleteImageAsync(
                currentPublicId,
                cancellationToken);
        }
    }
}
