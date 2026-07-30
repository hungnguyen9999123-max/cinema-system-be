namespace CinemaSystem.Common.Constants;

public static class PricingRuleDefaults
{
    public const decimal StandardBasePrice = 70_000m;
    public const decimal VipBasePrice = 90_000m;
    public const decimal ImaxBasePrice = 120_000m;
    public const decimal FourDxBasePrice = 150_000m;

    public const decimal NormalTimeMultiplier = 1.0m;
    public const decimal PeakTimeMultiplier = 1.2m;
    public const decimal EveningTimeMultiplier = 1.3m;
    public const decimal MidnightTimeMultiplier = 0.85m;

    public static readonly DateOnly DefaultEffectiveTo = new(2099, 12, 31);
}
