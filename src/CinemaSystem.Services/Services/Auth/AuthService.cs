using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Auth;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.EmailTemplates;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Services;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace CinemaSystem.Services.Services.Auth;

public sealed class AuthService : IAuthService
{
    private const int EmailVerificationLifetimeMinutes = 30;
    private const int DefaultMaxFailedLoginAttempts = 5;
    private const int DefaultAccountLockoutMinutes = 15;

    private const string DefaultBackendBaseUrl = "https://cinema-system-be.onrender.com";
    private const string DefaultFrontendBaseUrl = "https://215ca35c.cinema-system-fe.pages.dev";
    private const string DefaultVerificationSuccessUrl = "https://215ca35c.cinema-system-fe.pages.dev/successfully-verify";
    private const string DefaultVerificationFailureBaseUrl = "https://215ca35c.cinema-system-fe.pages.dev/verify-fail";

    private const string GoogleProvider = "GOOGLE";
    private const string LocalProvider = "LOCAL";

    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IEmailVerificationTokenRepository _emailVerificationTokenRepository;
    private readonly IPasswordResetTokenRepository _passwordResetTokenRepository;
    private readonly IJwtService _jwtService;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IEmailVerificationTokenRepository emailVerificationTokenRepository,
        IPasswordResetTokenRepository passwordResetTokenRepository,
        IJwtService jwtService,
        IConfiguration configuration,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _emailVerificationTokenRepository = emailVerificationTokenRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _jwtService = jwtService;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ApiResponse<RegisterResponseDto>> RegisterAsync(
        RegisterRequestDto request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var fullName = request.FullName.Trim();

        var existingUser = await _userRepository.GetByEmailAsync(email);
        if (existingUser is not null)
        {
            return ApiResponse<RegisterResponseDto>.Fail(CommonMessages.AlreadyExists);
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = fullName,
            Role = UserRole.Customer.ToString(),
            Status = "ACTIVE",
            IsEmailVerified = false,
            FailedLoginCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _userRepository.CreateAsync(user);

        var rawToken = CreateRawToken();
        var tokenHash = TokenHashHelper.Sha256(rawToken);
        var expiresAt = now.AddMinutes(EmailVerificationLifetimeMinutes);

        await _emailVerificationTokenRepository.CreateAsync(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsVerified = false
        });

        var verificationEmailSent = true;
        try
        {
            await SendVerificationEmailAsync(
                user.Email,
                user.FullName,
                rawToken,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            verificationEmailSent = false;
            _logger.LogError(
                ex,
                "Account {UserId} was created, but its verification email could not be sent.",
                user.Id);
            await _emailVerificationTokenRepository.InvalidateUnverifiedTokensAsync(user.Id);
        }

        return ApiResponse<RegisterResponseDto>.Success(new RegisterResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            EmailVerificationExpiresAt = expiresAt,
            VerificationEmailSent = verificationEmailSent
        }, verificationEmailSent
            ? CommonMessages.RegisterSuccess
            : "Account created, but the verification email could not be sent. Please request a new link.");
    }

    public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? clientIp = null)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
        if (user is null)
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.InvalidCredentials);
        }

        if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
        {
            _logger.LogWarning("Login attempt on locked account {Email}, locked until {LockedUntil}", user.Email, user.LockedUntil);
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.AccountLocked);
        }

        if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.InactiveAccount);
        }

        if (!user.IsEmailVerified)
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.EmailNotVerified);
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.FailedLoginCount = (byte)Math.Min(user.FailedLoginCount + 1, byte.MaxValue);
            var maxAttempts = GetMaxFailedLoginAttempts();
            if (user.FailedLoginCount >= maxAttempts)
            {
                user.LockedUntil = DateTime.UtcNow.AddMinutes(GetAccountLockoutMinutes());
                user.FailedLoginCount = 0;
                _logger.LogWarning("Account {Email} locked until {LockedUntil} after {Attempts} failed attempts.", user.Email, user.LockedUntil, maxAttempts);
            }
            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.InvalidCredentials);
        }

        user.FailedLoginCount = 0;
        user.LockedUntil = null;
        user.LastLogin = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, clientIp);

        var loginPair = await CreateTokenPairAsync(user, clientIp);
        return ApiResponse<LoginResponseDto>.Success(loginPair, CommonMessages.LoginSuccess);
    }

    public async Task<ApiResponse<GoogleLoginResponseDto>> GoogleLoginAsync(GoogleLoginRequest request, string? clientIp = null)
    {
        var googleClientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId))
        {
            _logger.LogError("Google sign-in is not configured. Google:ClientId is missing.");
            return ApiResponse<GoogleLoginResponseDto>.Fail("Google sign-in is not configured.");
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            payload = await GoogleJsonWebSignature.ValidateAsync(
                request.IdToken.Trim(),
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = [googleClientId]
                });
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Rejected an invalid Google ID token.");
            return ApiResponse<GoogleLoginResponseDto>.Fail(CommonMessages.InvalidGoogleToken);
        }

        var email = payload.Email?.Trim();
        var googleId = payload.Subject?.Trim();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(googleId) || payload.EmailVerified is false)
        {
            _logger.LogWarning("Google ID token did not contain a verified email and subject.");
            return ApiResponse<GoogleLoginResponseDto>.Fail(CommonMessages.InvalidGoogleToken);
        }

        var user = await _userRepository.GetByEmailAsync(email);
        var linkedGoogleUser = await _userRepository.GetByGoogleIdAsync(googleId);

        if (user is not null && !string.IsNullOrWhiteSpace(user.GoogleId) &&
            !string.Equals(user.GoogleId, googleId, StringComparison.Ordinal))
        {
            return ApiResponse<GoogleLoginResponseDto>.Fail(CommonMessages.EmailAlreadyLinkedToAnotherGoogleAccount);
        }

        if (linkedGoogleUser is not null && (user is null || linkedGoogleUser.Id != user.Id))
        {
            return ApiResponse<GoogleLoginResponseDto>.Fail(CommonMessages.GoogleAccountAlreadyLinked);
        }

        var now = DateTime.UtcNow;
        if (user is null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(CreateRawToken()),
                FullName = Truncate(payload.Name?.Trim() ?? email, 100),
                AvatarUrl = TruncateNullable(payload.Picture?.Trim(), 500),
                Provider = GoogleProvider,
                GoogleId = googleId,
                Role = UserRole.Customer.ToString(),
                Status = "ACTIVE",
                IsEmailVerified = true,
                FailedLoginCount = 0,
                LastLogin = now,
                CreatedAt = now,
                UpdatedAt = now
            };

            await _userRepository.CreateAsync(user);
        }
        else
        {
            if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<GoogleLoginResponseDto>.Fail(CommonMessages.InactiveAccount);
            }

            user.GoogleId ??= googleId;
            user.Provider ??= LocalProvider;
            user.IsEmailVerified = true;
            user.FailedLoginCount = 0;
            user.LastLogin = now;
            user.UpdatedAt = now;
            await _userRepository.UpdateAsync(user);
        }

        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, clientIp);

        var tokenPair = await CreateTokenPairAsync(user, clientIp);
        return ApiResponse<GoogleLoginResponseDto>.Success(new GoogleLoginResponseDto
        {
            AccessToken = tokenPair.AccessToken,
            RefreshToken = tokenPair.RefreshToken,
            User = new GoogleLoginUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                AvatarUrl = user.AvatarUrl,
                Provider = user.Provider ?? LocalProvider
            }
        }, CommonMessages.GoogleLoginSuccess);
    }

    public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? clientIp = null)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.Required);
        }

        var tokenHash = TokenHashHelper.Sha256(request.RefreshToken.Trim());
        var refreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);
        if (refreshToken is null)
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.InvalidToken);
        }

        if (refreshToken.IsRevoked)
        {
            await _refreshTokenRepository.RevokeAllUserTokensAsync(refreshToken.UserId, clientIp);
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.TokenReuseDetected);
        }

        if (refreshToken.ExpiresAt <= DateTime.UtcNow)
        {
            await _refreshTokenRepository.RevokeAsync(refreshToken, clientIp, tokenHash);
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.ExpiredToken);
        }

        var user = refreshToken.User ?? await _userRepository.GetByIdAsync(refreshToken.UserId);
        if (user is null)
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.InvalidToken);
        }

        if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) || !user.IsEmailVerified)
        {
            return ApiResponse<LoginResponseDto>.Fail(CommonMessages.AccountNotAllowed);
        }

        var loginPair = await RotateTokenPairAsync(user, refreshToken, clientIp);
        return ApiResponse<LoginResponseDto>.Success(loginPair, CommonMessages.TokenRefreshed);
    }

    public async Task<EmailVerificationResultDto> VerifyEmailAsync(VerifyEmailRequestDto request, string? clientIp = null)
    {
        var invalidRedirect = BuildVerificationFailureUrl("invalid");
        var expiredRedirect = BuildVerificationFailureUrl("expired");
        var successRedirect = BuildVerificationSuccessUrl();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new EmailVerificationResultDto
            {
                IsSuccess = false,
                Message = "Verification token is required.",
                RedirectUrl = invalidRedirect
            };

        }

        var incoming = request.Token.Trim();
        var tokenHash = TokenHashHelper.Sha256(incoming);

        var verificationToken = await _emailVerificationTokenRepository.GetByHashAsync(tokenHash);
        if (verificationToken is null)
        {
            return new EmailVerificationResultDto
            {
                IsSuccess = false,
                Message = "Invalid verification token.",
                RedirectUrl = invalidRedirect
            };

        }

        if (verificationToken.IsVerified)
        {
            return new EmailVerificationResultDto
            {
                IsSuccess = false,
                Message = "Verification token has already been used.",
                RedirectUrl = invalidRedirect
            };

        }

        if (verificationToken.ExpiresAt < DateTime.UtcNow)
        {

            return new EmailVerificationResultDto
            {
                IsSuccess = false,
                Message = "Verification token expired.",
                RedirectUrl = expiredRedirect
            };
        }

        var user = verificationToken.User ?? await _userRepository.GetByIdAsync(verificationToken.UserId);
        if (user is null)
        {
            return new EmailVerificationResultDto
            {
                IsSuccess = false,
                Message = "Invalid verification token.",
                RedirectUrl = invalidRedirect
            };
        }

        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        verificationToken.IsVerified = true;
        verificationToken.VerifiedAt = DateTime.UtcNow;
        verificationToken.VerifiedByIp = clientIp;
        await _emailVerificationTokenRepository.SaveChangesAsync();

        return new EmailVerificationResultDto
        {
            IsSuccess = true,
            Message = "Email verified successfully.",
            RedirectUrl = successRedirect
        };
    }

    public async Task<ApiResponse<ResendVerificationResponseDto>> ResendVerificationAsync(
        ResendVerificationRequestDto request,
        string? clientIp = null,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ApiResponse<ResendVerificationResponseDto>.Fail("Email is required.");
        }

        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            return ApiResponse<ResendVerificationResponseDto>.Fail("User not found.");
        }

        if (user.IsEmailVerified)
        {
            return ApiResponse<ResendVerificationResponseDto>.Fail("Email is already verified.");
        }

        var latestUnverifiedToken = await _emailVerificationTokenRepository.GetLatestUnverifiedByUserIdAsync(user.Id);
        if (latestUnverifiedToken is not null)
        {
            await _emailVerificationTokenRepository.InvalidateUnverifiedTokensAsync(user.Id);
        }

        var now = DateTime.UtcNow;
        var rawToken = CreateRawToken();
        var tokenHash = TokenHashHelper.Sha256(rawToken);
        var expiresAt = now.AddMinutes(EmailVerificationLifetimeMinutes);

        await _emailVerificationTokenRepository.CreateAsync(new EmailVerificationToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsVerified = false
        });

        try
        {
            await SendVerificationEmailAsync(
                user.Email,
                user.FullName,
                rawToken,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unable to resend verification email for user {UserId}.",
                user.Id);
            await _emailVerificationTokenRepository.InvalidateUnverifiedTokensAsync(user.Id);
            return ApiResponse<ResendVerificationResponseDto>.Fail(
                "Unable to send verification email. Please try again later.");
        }

        return ApiResponse<ResendVerificationResponseDto>.Success(new ResendVerificationResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            EmailVerificationExpiresAt = expiresAt,
            VerificationEmailSent = true
        }, "Verification email resent successfully.");

    }

    public async Task<ApiResponse<string>> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim();
        var user = await _userRepository.GetByEmailAsync(email);
        if (user is null)
        {
            return ApiResponse<string>.Success(string.Empty, "If the email exists, reset instructions have been sent.");
        }

        var now = DateTime.UtcNow;
        var rawToken = CreateRawToken();
        var tokenHash = TokenHashHelper.Sha256(rawToken);
        var expiresAt = now.AddMinutes(15);

        await _passwordResetTokenRepository.CreateAsync(new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = expiresAt,
            IsUsed = false
        });

        var resetLink = BuildResetPasswordLink(rawToken);
        var htmlBody = $"<p>Hi {user.FullName},</p><p>Please click the following link to reset your password:</p><p><a href='{resetLink}'>Reset Password</a></p>";
        
        try
        {
            await _emailService.SendEmailAsync(
                user.Email,
                "Reset your password",
                htmlBody,
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unable to send password reset email for user {UserId}.",
                user.Id);
        }

        return ApiResponse<string>.Success(string.Empty, "If the email exists, reset instructions have been sent.");
    }

    public async Task<ApiResponse<string>> ResetPasswordAsync(ResetPasswordRequestDto request, string? clientIp = null)
    {
        var tokenHash = TokenHashHelper.Sha256(request.Token.Trim());
        var resetToken = await _passwordResetTokenRepository.GetByHashAsync(tokenHash);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTime.UtcNow)
        {
            return ApiResponse<string>.Fail("Invalid or expired reset token.");
        }

        var user = resetToken.User ?? await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user is null)
        {
            return ApiResponse<string>.Fail("Invalid or expired reset token.");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);

        resetToken.IsUsed = true;
        resetToken.UsedAt = DateTime.UtcNow;
        resetToken.UsedByIp = clientIp;
        await _passwordResetTokenRepository.UpdateAsync(resetToken);

        await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, clientIp);

        return ApiResponse<string>.Success(string.Empty, "Password reset successfully.");
    }

    private async Task<LoginResponseDto> CreateTokenPairAsync(User user, string? clientIp)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var accessTokenExpiresAt = DateTime.UtcNow.AddHours(GetAccessTokenLifetimeHours());
        var refreshToken = _jwtService.GenerateRefreshToken();
        var refreshTokenHash = TokenHashHelper.Sha256(refreshToken);
        var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays());

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = refreshTokenExpiresAt,
            CreatedByIp = clientIp,
            IsRevoked = false
        });

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = NormalizeRole(user.Role),
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt
        };
    }

    private async Task<LoginResponseDto> RotateTokenPairAsync(User user, RefreshToken currentToken, string? clientIp)
    {
        var accessToken = _jwtService.GenerateAccessToken(user);
        var accessTokenExpiresAt = DateTime.UtcNow.AddHours(GetAccessTokenLifetimeHours());
        var newRefreshToken = _jwtService.GenerateRefreshToken();
        var newRefreshTokenHash = TokenHashHelper.Sha256(newRefreshToken);
        var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays());

        await _refreshTokenRepository.RevokeAsync(currentToken, clientIp, newRefreshTokenHash);

        await _refreshTokenRepository.CreateAsync(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = newRefreshTokenExpiresAt,
            CreatedByIp = clientIp,
            IsRevoked = false
        });

        return new LoginResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = NormalizeRole(user.Role),
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            AccessTokenExpiresAt = accessTokenExpiresAt
        };
    }

    private async Task SendVerificationEmailAsync(
        string email,
        string fullName,
        string rawToken,
        CancellationToken cancellationToken)
    {
        var verificationLink = BuildVerificationLink(rawToken);
        var htmlBody = VerificationEmailTemplate.Build(fullName, verificationLink);

        _logger.LogInformation("Sending verification email to {Email}", email);
        try
        {
            await _emailService.SendEmailAsync(
                email,
                "Verify your email",
                htmlBody,
                cancellationToken);

            _logger.LogInformation("Verification email sent to {Email}", email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send verification email to {Email}", email);
            throw;
        }
    }

    private string BuildVerificationLink(string rawToken)
    {
        var baseUrl = GetBackendBaseUrl();
        return $"{baseUrl.TrimEnd('/')}/api/auth/verify-email?token={Uri.EscapeDataString(rawToken)}";
    }

    private string BuildResetPasswordLink(string rawToken)
    {
        var baseUrl = GetFrontendBaseUrl();
        return $"{baseUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(rawToken)}";
    }

    private string BuildVerificationSuccessUrl() =>
        _configuration["App:EmailVerificationSuccessUrl"] ?? DefaultVerificationSuccessUrl;

    private string BuildVerificationFailureUrl(string reason)
    {
        var baseUrl = _configuration["App:EmailVerificationFailedUrl"] ?? DefaultVerificationFailureBaseUrl;
        return $"{baseUrl.TrimEnd('/')}?reason={Uri.EscapeDataString(reason)}";
    }

    private string GetFrontendBaseUrl() =>
        _configuration["App:FrontendBaseUrl"] ?? DefaultFrontendBaseUrl;

    private string GetBackendBaseUrl() =>
        _configuration["App:BaseUrl"] ?? DefaultBackendBaseUrl;

    private int GetAccessTokenLifetimeHours() =>
        int.TryParse(_configuration["Jwt:AccessTokenExpirationHours"], out var hours) ? hours : 2;

    private int GetRefreshTokenLifetimeDays() =>
        int.TryParse(_configuration["Jwt:RefreshTokenExpirationDays"], out var days) ? days : 30;

    private int GetMaxFailedLoginAttempts() =>
        int.TryParse(_configuration["Auth:MaxFailedLoginAttempts"], out var attempts) && attempts > 0
            ? attempts
            : DefaultMaxFailedLoginAttempts;

    private int GetAccountLockoutMinutes() =>
        int.TryParse(_configuration["Auth:AccountLockoutMinutes"], out var minutes) && minutes > 0
            ? minutes
            : DefaultAccountLockoutMinutes;

    private static string CreateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? TruncateNullable(string? value, int maxLength) =>
        string.IsNullOrWhiteSpace(value) ? null : Truncate(value, maxLength);

    private static string NormalizeRole(string role)
    {
        return Enum.TryParse<UserRole>(role, true, out var parsedRole)
            ? parsedRole.ToString()
            : UserRole.Customer.ToString();
    }
}
