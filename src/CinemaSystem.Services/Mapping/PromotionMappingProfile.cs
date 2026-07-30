using AutoMapper;
using CinemaSystem.Common.DTOs.Promotions;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Mapping;

/// <summary>
/// AutoMapper profile for promotion entities and DTOs.
/// </summary>
public sealed class PromotionMappingProfile : Profile
{
    public PromotionMappingProfile()
    {
        CreateMap<Promotion, PromotionResponse>();

        CreateMap<CreatePromotionRequest, Promotion>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.PromoCode, opt => opt.MapFrom(src => src.PromoCode.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.DiscountType.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom))
            .ForMember(dest => dest.ValidTo, opt => opt.MapFrom(src => src.ValidTo))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByNavigation, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionUsages, opt => opt.Ignore());

        CreateMap<UpdatePromotionRequest, Promotion>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.PromoCode, opt => opt.MapFrom(src => src.PromoCode.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
            .ForMember(dest => dest.DiscountType, opt => opt.MapFrom(src => src.DiscountType.Trim().ToUpperInvariant()))
            .ForMember(dest => dest.ValidFrom, opt => opt.MapFrom(src => src.ValidFrom))
            .ForMember(dest => dest.ValidTo, opt => opt.MapFrom(src => src.ValidTo))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Bookings, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedByNavigation, opt => opt.Ignore())
            .ForMember(dest => dest.PromotionUsages, opt => opt.Ignore());

        CreateMap<PromotionUsage, PromotionUsageResponse>()
            .ForMember(dest => dest.PromoCode, opt => opt.MapFrom(src => src.Promotion.PromoCode))
            .ForMember(dest => dest.PromotionName, opt => opt.MapFrom(src => src.Promotion.Name));
    }
}
