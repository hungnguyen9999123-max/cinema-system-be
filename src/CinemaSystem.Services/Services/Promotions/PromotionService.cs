using AutoMapper;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Promotions;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Promotions;

/// <summary>
/// Implements promotion business rules and statistics.
/// </summary>
public sealed class PromotionService : IPromotionService
{
    private const string PercentageDiscount = "PERCENTAGE";
    private const string FixedAmountDiscount = "FIXED_AMOUNT";
    private const string LegacyAmountDiscount = "AMOUNT";

    private readonly IPromotionRepository promotionRepository;
    private readonly IPromotionUsageRepository promotionUsageRepository;
    private readonly IBookingRepository bookingRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IMapper mapper;

    public PromotionService(
        IPromotionRepository promotionRepository,
        IPromotionUsageRepository promotionUsageRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        this.promotionRepository = promotionRepository;
        this.promotionUsageRepository = promotionUsageRepository;
        this.bookingRepository = bookingRepository;
        this.unitOfWork = unitOfWork;
        this.mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionResponse>> GetAllAsync(
        string? search,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        var query = promotionRepository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(p =>
                p.PromoCode.Contains(keyword) ||
                p.Name.Contains(keyword));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var promotions = await query
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return mapper.Map<List<PromotionResponse>>(promotions);
    }

    /// <inheritdoc />
    public async Task<PromotionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken);
        return promotion is null ? null : mapper.Map<PromotionResponse>(promotion);
    }

    /// <inheritdoc />
    public async Task<PromotionResponse> CreateAsync(
        Guid createdBy,
        CreatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await EnsurePromoCodeIsUniqueAsync(request.PromoCode, null, cancellationToken);

            var promotion = mapper.Map<Promotion>(request);
            promotion.Id = Guid.NewGuid();
            promotion.CreatedBy = createdBy;
            promotion.CreatedAt = DateTime.UtcNow;
            promotion.PromoCode = NormalizePromoCode(request.PromoCode);
            promotion.DiscountType = NormalizeDiscountType(request.DiscountType);

            ValidateDiscountRules(promotion.DiscountType, promotion.DiscountValue, promotion.ValidFrom, promotion.ValidTo, promotion.UsageLimit);
            await promotionRepository.AddAsync(promotion, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return mapper.Map<PromotionResponse>(promotion);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PromotionResponse?> UpdateAsync(
        Guid id,
        UpdatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken);
            if (promotion is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return null;
            }

            await EnsurePromoCodeIsUniqueAsync(request.PromoCode, id, cancellationToken);

            mapper.Map(request, promotion);
            promotion.PromoCode = NormalizePromoCode(request.PromoCode);
            promotion.DiscountType = NormalizeDiscountType(request.DiscountType);

            ValidateDiscountRules(promotion.DiscountType, promotion.DiscountValue, promotion.ValidFrom, promotion.ValidTo, promotion.UsageLimit);

            promotionRepository.Update(promotion);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return mapper.Map<PromotionResponse>(promotion);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PromotionResponse?> ActivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await SetActiveStateAsync(id, true, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PromotionResponse?> DeactivateAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await SetActiveStateAsync(id, false, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken);
            if (promotion is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return false;
            }

            var usageCount = await promotionUsageRepository.CountByPromotionIdAsync(id, cancellationToken);
            var isReferencedByBookings = await bookingRepository.Query()
                .AnyAsync(booking => booking.PromotionId == id, cancellationToken);

            if (usageCount > 0)
            {
                throw new BusinessConflictException(PromotionMessages.CannotDeleteBecauseUsed);
            }

            if (isReferencedByBookings)
            {
                throw new BusinessConflictException(PromotionMessages.CannotDeleteBecauseReferenced);
            }

            promotionRepository.Delete(promotion);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return true;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<PromotionStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var promotions = await promotionRepository.Query()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usages = await promotionUsageRepository.Query()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var bookings = await bookingRepository.Query()
            .AsNoTracking()
            .Where(booking => booking.PromotionId != null)
            .ToListAsync(cancellationToken);

        var totalPromotions = promotions.Count;
        var activePromotions = promotions.Count(promotion =>
            promotion.IsActive &&
            promotion.ValidFrom <= today &&
            promotion.ValidTo >= today);
        var expiredPromotions = promotions.Count(promotion => promotion.ValidTo < today);
        var totalUsages = usages.Count;
        var totalDiscountAmount = bookings.Sum(item => item.TotalAmount - item.FinalAmount);

        var usageStats = promotions
            .GroupJoin(
                usages,
                promotion => promotion.Id,
                usage => usage.PromotionId,
                (promotion, promotionUsages) => new PromotionUsageStatisticsItem(
                    promotion.Id,
                    promotion.PromoCode,
                    promotion.Name,
                    promotionUsages.Count(),
                    bookings.Where(booking => booking.PromotionId == promotion.Id)
                        .Sum(booking => booking.TotalAmount - booking.FinalAmount)))
            .OrderByDescending(item => item.UsageCount)
            .ThenBy(item => item.PromoCode)
            .ToList();

        var bookingGroups = bookings
            .GroupBy(booking => booking.PromotionId!.Value)
            .ToDictionary(group => group.Key, group => group.ToList());

        var revenueStats = promotions
            .Select(promotion =>
            {
                bookingGroups.TryGetValue(promotion.Id, out var promotionBookings);
                promotionBookings ??= new List<Booking>();

                return new PromotionRevenueStatisticsItem(
                    promotion.Id,
                    promotion.PromoCode,
                    promotion.Name,
                    promotionBookings.Count,
                    promotionBookings.Sum(item => item.FinalAmount),
                    promotionBookings.Sum(item => item.TotalAmount - item.FinalAmount));
            })
            .OrderByDescending(item => item.UsageCount)
            .ThenBy(item => item.PromoCode)
            .ToList();

        return new PromotionStatisticsResponse
        {
            TotalPromotions = totalPromotions,
            ActivePromotions = activePromotions,
            ExpiredPromotions = expiredPromotions,
            TotalUsages = totalUsages,
            TotalDiscountAmount = totalDiscountAmount,
            MostUsedPromotions = usageStats,
            RevenueByPromotion = revenueStats
        };
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PromotionUsageResponse>> GetUsagesAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var promotion = await promotionRepository.GetByIdAsync(promotionId, cancellationToken);
        if (promotion is null)
        {
            throw new KeyNotFoundException(PromotionMessages.NotFound);
        }

        var usages = await promotionUsageRepository.GetByPromotionIdAsync(promotionId, cancellationToken);
        return mapper.Map<List<PromotionUsageResponse>>(usages);
    }

    /// <inheritdoc />
    public Task<ValidatePromotionResponse> ValidateAsync(
        ValidatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        return ValidateAsync(null, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ValidatePromotionResponse> ValidateAsync(
        Guid? customerId,
        ValidatePromotionRequest request,
        CancellationToken cancellationToken = default)
    {
        var promoCode = NormalizePromoCode(request.PromoCode);
        var promotion = await promotionRepository.GetByCodeAsync(promoCode, cancellationToken);

        if (promotion is null)
        {
            return BuildInvalidResponse(PromotionMessages.PromotionCodeNotFound, request.BookingAmount);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (!promotion.IsActive)
        {
            return BuildInvalidResponse(PromotionMessages.Inactive, request.BookingAmount, promotion.Id);
        }

        if (today < promotion.ValidFrom)
        {
            return BuildInvalidResponse(PromotionMessages.NotStarted, request.BookingAmount, promotion.Id);
        }

        if (today > promotion.ValidTo)
        {
            return BuildInvalidResponse(PromotionMessages.Expired, request.BookingAmount, promotion.Id);
        }

        if (promotion.MinOrderAmt.HasValue && request.BookingAmount < promotion.MinOrderAmt.Value)
        {
            return BuildInvalidResponse(PromotionMessages.MinimumOrderNotMet, request.BookingAmount, promotion.Id);
        }

        var usageCount = await promotionUsageRepository.CountByPromotionIdAsync(promotion.Id, cancellationToken);
        if (promotion.UsageLimit.HasValue && usageCount >= promotion.UsageLimit.Value)
        {
            return BuildInvalidResponse(PromotionMessages.UsageLimitExceeded, request.BookingAmount, promotion.Id);
        }

        var discountAmount = CalculateDiscount(promotion, request.BookingAmount);
        var finalAmount = Math.Max(0m, request.BookingAmount - discountAmount);

        return new ValidatePromotionResponse(
            true,
            promotion.Id,
            discountAmount,
            finalAmount,
            PromotionMessages.ValidationSucceeded);
    }

    /// <summary>
    /// Records a promotion usage after successful payment.
    /// </summary>
    public async Task RecordUsageAsync(Guid bookingId, Guid? customerId, Guid promotionId, CancellationToken cancellationToken = default)
    {
        if (await promotionUsageRepository.ExistsForBookingAsync(bookingId, cancellationToken))
        {
            return;
        }

        await promotionUsageRepository.AddAsync(new PromotionUsage
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            CustomerId = customerId,
            PromotionId = promotionId,
            UsedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    private async Task EnsurePromoCodeIsUniqueAsync(string promoCode, Guid? excludedPromotionId, CancellationToken cancellationToken)
    {
        var normalized = NormalizePromoCode(promoCode);
        var exists = await promotionRepository.CodeExistsAsync(normalized, excludedPromotionId, cancellationToken);
        if (exists)
        {
            throw new BusinessConflictException(PromotionMessages.CodeAlreadyExists);
        }
    }

    private async Task<PromotionResponse?> SetActiveStateAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var promotion = await promotionRepository.GetByIdAsync(id, cancellationToken);
            if (promotion is null)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return null;
            }

            promotion.IsActive = isActive;
            promotionRepository.Update(promotion);

            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return mapper.Map<PromotionResponse>(promotion);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static void ValidateDiscountRules(
        string discountType,
        decimal discountValue,
        DateOnly validFrom,
        DateOnly validTo,
        int? usageLimit)
    {
        if (discountValue <= 0)
        {
            throw new InvalidOperationException(PromotionMessages.InvalidDiscountValue);
        }

        if (discountType.Equals(PercentageDiscount, StringComparison.OrdinalIgnoreCase) && discountValue > 100)
        {
            throw new InvalidOperationException(PromotionMessages.InvalidPercentageDiscount);
        }

        if (validFrom > validTo)
        {
            throw new InvalidOperationException(PromotionMessages.InvalidDateRange);
        }

        if (usageLimit.HasValue && usageLimit.Value <= 0)
        {
            throw new InvalidOperationException(PromotionMessages.InvalidUsageLimit);
        }
    }

    private static decimal CalculateDiscount(Promotion promotion, decimal bookingAmount)
    {
        if (promotion.DiscountType.Equals(PercentageDiscount, StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(bookingAmount * promotion.DiscountValue / 100m, 2, MidpointRounding.AwayFromZero);
        }

        if (promotion.DiscountType.Equals(FixedAmountDiscount, StringComparison.OrdinalIgnoreCase) ||
            promotion.DiscountType.Equals(LegacyAmountDiscount, StringComparison.OrdinalIgnoreCase))
        {
            return Math.Round(Math.Min(promotion.DiscountValue, bookingAmount), 2, MidpointRounding.AwayFromZero);
        }

        throw new InvalidOperationException(PromotionMessages.InvalidDiscountType);
    }

    private static ValidatePromotionResponse BuildInvalidResponse(string message, decimal bookingAmount, Guid? promotionId = null)
    {
        return new ValidatePromotionResponse(
            false,
            promotionId,
            0m,
            bookingAmount,
            message);
    }

    private static string NormalizePromoCode(string promoCode)
        => promoCode.Trim().ToUpperInvariant();

    private static string NormalizeDiscountType(string discountType)
    {
        var normalized = discountType.Trim().ToUpperInvariant();
        return normalized == LegacyAmountDiscount ? FixedAmountDiscount : normalized;
    }
}
