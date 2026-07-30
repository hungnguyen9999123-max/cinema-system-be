namespace CinemaSystem.Common.Constants;

public static class UploadFolders
{
    public const string MoviePosters = "movies/posters";
    public const string MovieBanners = "movies/banners";
    public const string Fnb = "fnb";
    public const string Promotions = "promotions";

    public static readonly IReadOnlySet<string> Allowed =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            MoviePosters,
            MovieBanners,
            Fnb,
            Promotions
        };
}
