using System.Text;
using System.Threading.RateLimiting;
using CinemaSystem.API.Middleware;
using CinemaSystem.API.Services;
using CinemaSystem.Common.Services;
using CinemaSystem.Common.Settings;
using CinemaSystem.API.Validators.Auth;
using CinemaSystem.API.Validators.Rooms;
using CinemaSystem.API.Validators.Showtimes;
using CinemaSystem.API.Validators.Pos;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.DAL.Repository.Auth;
using CinemaSystem.DAL.Repository.Cinemas;
using CinemaSystem.DAL.Repository.Fnb;
using CinemaSystem.DAL.Repository.Rooms;
using CinemaSystem.DAL.Repository.Seats;
using CinemaSystem.DAL.Repository.PricingRules;
using CinemaSystem.DAL.Repository.AudienceTypes;
using CinemaSystem.DAL.Repository.Showtimes;
using CinemaSystem.DAL.Repositories.Movies;
using CinemaSystem.Services.Services.Auth;
using CinemaSystem.Services.Services.Cinemas;
using CinemaSystem.Services.Services.Fnb;
using CinemaSystem.Services.Services.Movies;
using CinemaSystem.Services.Services.Rooms;
using CinemaSystem.Services.Services.PricingRules;
using CinemaSystem.Services.Services.Showtimes;
using CinemaSystem.DAL.Repository.Bookings;
using CinemaSystem.DAL.Repository.Promotions;
using CinemaSystem.DAL.Repository.Payments;
using CinemaSystem.DAL.Repository.QrTickets;
using CinemaSystem.Services.Services.Bookings;
using CinemaSystem.Services.Services.Promotions;
using CinemaSystem.Services.Services.QrTickets;
using CinemaSystem.Services.Services.Payments;
using CinemaSystem.Services.Services.FnbPayments;
using CinemaSystem.Services.Services.Pos;
using CinemaSystem.Services.Services.Recommendations;
using CinemaSystem.DAL.Repository.Reports;
using CinemaSystem.DAL.Repository.Refunds;
using CinemaSystem.DAL.Repository.Wallets;
using CinemaSystem.Services.Services.Reports;
using CinemaSystem.Services.Services.Refunds;
using CinemaSystem.Services.Services.Wallets;
using CinemaSystem.DAL.Repository.Users;
using CinemaSystem.Services.Services.AdminUsers;
using CinemaSystem.DAL.Infrastructure;
using CinemaSystem.API.Services.BackgroundJobs;
using CinemaSystem.Services.Services.Uploads;
using CinemaSystem.Services.Mapping;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var refundIpLimit = builder.Configuration.GetValue<int?>("Refunds:IpRequestsPerHour") ?? 10;
const string CorsPolicy = "CinemaSystemCors";

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("App:Cors:AllowedOrigins")
            .Get<string[]>()?
            .Select(origin => origin.TrimEnd('/'))
            .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                var normalizedOrigin = origin.TrimEnd('/');
                if (allowedOrigins.Contains(normalizedOrigin))
                {
                    return true;
                }

                var isCloudflarePagesOrigin =
                    uri.Scheme == Uri.UriSchemeHttps
                    && (uri.Host.Equals(
                            "cinema-system-fe.pages.dev",
                            StringComparison.OrdinalIgnoreCase)
                        || uri.Host.EndsWith(
                            ".cinema-system-fe.pages.dev",
                            StringComparison.OrdinalIgnoreCase));

                var isLocalDevelopmentOrigin =
                    builder.Environment.IsDevelopment()
                    && uri.Scheme == Uri.UriSchemeHttp
                    && uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
                    && uri.Port == 5173;

                return isCloudflarePagesOrigin || isLocalDevelopmentOrigin;
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
builder.Services.AddAutoMapper(cfg => cfg.AddMaps(typeof(PromotionMappingProfile).Assembly));
builder.Services.AddHttpContextAccessor();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("refund-ip", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = refundIpLimit,
                Window = TimeSpan.FromHours(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection(CloudinarySettings.SectionName));

builder.Services.AddDbContext<CinemaDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(0)
    );
});

builder.Services.AddScoped<IMovieRepository, MovieRepository>();
builder.Services.AddScoped<IMovieService, MovieService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICinemaRepository, CinemaRepository>();
builder.Services.AddScoped<ICinemaService, CinemaService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<ISeatRepository, SeatRepository>();
builder.Services.AddScoped<ISeatTypeRepository, SeatTypeRepository>();
builder.Services.AddScoped<IShowtimeRepository, ShowtimeRepository>();
builder.Services.AddScoped<IFnbItemRepository, FnbItemRepository>();
builder.Services.AddScoped<IFnbOrderRepository, FnbOrderRepository>();
builder.Services.AddScoped<IFnbOrderDetailRepository, FnbOrderDetailRepository>();
builder.Services.AddScoped<IPricingRuleRepository, PricingRuleRepository>();
builder.Services.AddScoped<IAudienceTypeRepository, AudienceTypeRepository>();
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionUsageRepository, PromotionUsageRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<ISeatService, SeatService>();
builder.Services.AddScoped<IShowtimeService, ShowtimeService>();
builder.Services.AddScoped<IFnbItemService, FnbItemService>();
builder.Services.AddScoped<IFnbOrderService, FnbOrderService>();
builder.Services.AddScoped<IPricingRuleService, PricingRuleService>();
builder.Services.AddScoped<IPromotionService, PromotionService>();
builder.Services.AddScoped<ITicketPricingService, TicketPricingService>();
builder.Services.AddSingleton<ICloudinaryService, CloudinaryService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IUserRepository, UserResponsitory>();
builder.Services.AddScoped<IAdminUserRepository, AdminUserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminUserService, AdminUserService>();

// Booking Module
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<IQrTicketRepository, QrTicketRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IFnbPaymentService, FnbPaymentService>();
builder.Services.AddScoped<IQrTicketService, QrTicketService>();
builder.Services.AddScoped<IPosBookingService, PosBookingService>();
builder.Services.AddScoped<IPosBookingConfirmationService, PosBookingConfirmationService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IRefundService, RefundService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<IWalletTopUpService, WalletTopUpService>();
builder.Services.AddScoped<IRefundAuditService, RefundAuditService>();
builder.Services.AddScoped<IRefundNotificationService, RefundNotificationService>();

builder.Services.AddHostedService<BookingExpiryBackgroundService>();
builder.Services.AddHostedService<ShowtimeCompletionBackgroundService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? string.Empty)),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext =>
    {
        var clientIp = ResolveIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.AddPolicy("auth-strict", httpContext =>
    {
        var clientIp = ResolveIpAddress(httpContext);
        return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });

    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        var payload = System.Text.Json.JsonSerializer.Serialize(
            new { isSuccess = false, message = "Too many requests. Please slow down." });
        await context.HttpContext.Response.WriteAsync(payload, cancellationToken);
    };
});

static string ResolveIpAddress(HttpContext httpContext)
{
    if (httpContext.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
    {
        var first = forwarded.ToString()
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(first))
            return first;
    }
    return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cinema System API",
        Version = "v1",
        Description = "Cinema management API with JWT and refresh token authentication."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token. The 'Bearer' prefix is added automatically."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

//if (app.Environment.IsDevelopment())
//{
//    //    app.MapOpenApi();
//    //    app.UseSwagger();
//    //    app.UseSwaggerUI(options =>
//    //    {
//    //        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema System API v1");
//    //        options.RoutePrefix = "swagger";
//    //        options.DocumentTitle = "Cinema System API";
//    //    });
//    app.MapOpenApi();

//app.UseSwagger();

//app.UseSwaggerUI(options =>
//{
//    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema System API v1");
//    options.RoutePrefix = "swagger";
//    options.DocumentTitle = "Cinema System API";
//});
//}
if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.MapOpenApi();

    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Cinema System API v1");
        options.RoutePrefix = "swagger";
        options.DocumentTitle = "Cinema System API";
    });
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors(CorsPolicy);
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Urls.Clear();
app.Urls.Add($"http://0.0.0.0:{port}");
