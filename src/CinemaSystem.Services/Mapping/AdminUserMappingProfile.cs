using AutoMapper;
using CinemaSystem.Common.DTOs.AdminUsers;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Mapping;

/// <summary>
/// Defines mappings between user entities and administrative user DTOs.
/// </summary>
public sealed class AdminUserMappingProfile : Profile
{
    /// <summary>
    /// Initializes the mapping configuration.
    /// </summary>
    public AdminUserMappingProfile()
    {
        CreateMap<User, UserResponseDto>();
        CreateMap<User, UserDetailResponseDto>();

        CreateMap<UpdateUserRequestDto, User>()
            .ForMember(destination => destination.Id, options => options.Ignore())
            .ForMember(destination => destination.Email, options => options.Ignore())
            .ForMember(destination => destination.PasswordHash, options => options.Ignore())
            .ForMember(destination => destination.Role, options => options.Ignore())
            .ForMember(destination => destination.Status, options => options.Ignore())
            .ForMember(destination => destination.IsEmailVerified, options => options.Ignore())
            .ForMember(destination => destination.LastLogin, options => options.Ignore())
            .ForMember(destination => destination.FailedLoginCount, options => options.Ignore())
            .ForMember(destination => destination.LockedUntil, options => options.Ignore())
            .ForMember(destination => destination.CreatedAt, options => options.Ignore())
            .ForMember(destination => destination.UpdatedAt, options => options.Ignore())
            .ForMember(destination => destination.Provider, options => options.Ignore())
            .ForMember(destination => destination.GoogleId, options => options.Ignore())
            .ForMember(destination => destination.AuditLogs, options => options.Ignore())
            .ForMember(destination => destination.Bookings, options => options.Ignore())
            .ForMember(destination => destination.EmailVerificationToken, options => options.Ignore())
            .ForMember(destination => destination.Feedbacks, options => options.Ignore())
            .ForMember(destination => destination.FnbItems, options => options.Ignore())
            .ForMember(destination => destination.FnbOrders, options => options.Ignore())
            .ForMember(destination => destination.Movies, options => options.Ignore())
            .ForMember(destination => destination.Notifications, options => options.Ignore())
            .ForMember(destination => destination.PasswordResetToken, options => options.Ignore())
            .ForMember(destination => destination.PromotionUsages, options => options.Ignore())
            .ForMember(destination => destination.Promotions, options => options.Ignore())
            .ForMember(destination => destination.RefreshToken, options => options.Ignore())
            .ForMember(destination => destination.Refunds, options => options.Ignore())
            .ForMember(destination => destination.Showtimes, options => options.Ignore())
            .ForMember(destination => destination.StaffAssignments, options => options.Ignore())
            .ForMember(destination => destination.Tickets, options => options.Ignore())
            .ForMember(destination => destination.FullName, options => options.MapFrom(source => source.FullName.Trim()))
            .ForMember(destination => destination.Phone, options => options.MapFrom(source => NormalizeOptional(source.Phone)))
            .ForMember(destination => destination.AvatarUrl, options => options.MapFrom(source => NormalizeOptional(source.AvatarUrl)));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
