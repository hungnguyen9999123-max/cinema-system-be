# Cinema System - Tài Liệu Chi Tiết Về Luồng Chạy và Kiến Trúc

## 📋 Mục lục

1. [Tổng Quan Kiến Trúc](#tổng-quan-kiến-trúc)
2. [Các Package/Lớp Chính](#các-packagelớp-chính)
3. [Luồng Chạy REGISTER](#luồng-chạy-register)
4. [Luồng Chạy LOGIN](#luồng-chạy-login)
5. [Luồng Chạy EMAIL VERIFICATION](#luồng-chạy-email-verification)
6. [Luồng Chạy REFRESH TOKEN](#luồng-chạy-refresh-token)
7. [Chi Tiết Các Lớp và Phương Thức](#chi-tiết-các-lớp-và-phương-thức)

---

## 🏗️ Tổng Quan Kiến Trúc

Hệ thống Cinema System sử dụng **kiến trúc phân lớp** (Layered Architecture) với 4 lớp chính:

```
┌─────────────────────────────────────┐
│   CinemaSystem.API (Presentation)   │
│   - Controllers                     │
│   - Middleware                      │
│   - Services (EmailService)         │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  CinemaSystem.Services (Business)   │
│  - AuthService                      │
│  - JwtService                       │
│  - MovieService                     │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│  CinemaSystem.DAL (Data Access)     │
│  - Repositories                     │
│  - DbContext (EF Core)              │
│  - Models/Entities                  │
└──────────────┬──────────────────────┘
               │
┌──────────────▼──────────────────────┐
│   CinemaSystem.Common (Shared)      │
│  - DTOs                             │
│  - Constants                        │
│  - Enums                            │
│  - Interfaces                       │
└─────────────────────────────────────┘
```

**Quy tắc Luồng Dữ Liệu:**

- **Chiều xuống**: Request từ Client → API → Services → DAL → Database
- **Chiều lên**: Response từ Database → DAL → Services → API → Client
- **Dependency Injection**: Các lớp trên có thể gọi các lớp dưới, nhưng không được ngược lại

---

## 📦 Các Package/Lớp Chính

### 1. **CinemaSystem.API** (Presentation Layer)

**Mục đích**: Xử lý HTTP requests/responses, điều hướng đến các dịch vụ

**Các thành phần chính:**

| Thành Phần                       | Mục Đích                 | Chi Tiết                                              |
| -------------------------------- | ------------------------ | ----------------------------------------------------- |
| **AuthController.cs**            | Xử lý các endpoint auth  | POST /register, /login, /refresh-token, /verify-email |
| **MoviesController.cs**          | Xử lý các endpoint movie | CRUD operations cho movies                            |
| **GlobalExceptionMiddleware.cs** | Xử lý lỗi toàn cục       | Catch tất cả exceptions và trả về error response      |
| **EmailService.cs**              | Gửi email                | Kết nối SMTP, gửi verification emails                 |

**Program.cs - Dependency Injection Setup:**

```csharp
// Database
builder.Services.AddDbContext<CinemaDbContext>(options =>
    options.UseSqlServer(connectionString));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Authentication & Authorization
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        // JWT validation configuration
    });
builder.Services.AddAuthorization();
```

---

### 2. **CinemaSystem.Services** (Business Logic Layer)

**Mục đích**: Chứa toàn bộ logic business, xử lý data transformation, validation

**Các Service chính:**

#### **a) AuthService.cs**

**Trách nhiệm:**

- Xử lý đăng ký (Register)
- Xử lý đăng nhập (Login)
- Xác minh email (Verify Email)
- Làm mới token (Refresh Token)

**Các Phương Thức Chính:**

| Phương Thức           | Tham Số                          | Trả Về                           | Mô Tả                                   |
| --------------------- | -------------------------------- | -------------------------------- | --------------------------------------- |
| `RegisterAsync()`     | RegisterRequestDto, clientIp     | ApiResponse<RegisterResponseDto> | Tạo user mới, gửi email verification    |
| `LoginAsync()`        | LoginRequestDto, clientIp        | ApiResponse<LoginResponseDto>    | Kiểm tra email/password, tạo token pair |
| `VerifyEmailAsync()`  | VerifyEmailRequestDto, clientIp  | ApiResponse<string>              | Xác nhận email từ token                 |
| `RefreshTokenAsync()` | RefreshTokenRequestDto, clientIp | ApiResponse<LoginResponseDto>    | Cấp access token mới                    |

**Phương Thức Nội Bộ:**

```csharp
// Tạo cặp token (Access + Refresh)
private async Task<LoginResponseDto> CreateTokenPairAsync(User user, string? clientIp)

// Xoay token (revoke cũ, tạo mới)
private async Task<LoginResponseDto> RotateTokenPairAsync(User user, RefreshToken currentToken, string? clientIp)

// Lấy cấu hình từ appsettings.json
private int GetAccessTokenLifetimeHours()
private int GetRefreshTokenLifetimeDays()

// Tạo token random 64 bytes
private static string CreateRawToken()

// Chuẩn hóa role
private static string NormalizeRole(string role)
```

#### **b) JwtService.cs**

**Trách nhiệm:** Tạo và quản lý JWT tokens

**Các Phương Thức:**

| Phương Thức              | Tham Số | Trả Về | Mô Tả                                 |
| ------------------------ | ------- | ------ | ------------------------------------- |
| `GenerateAccessToken()`  | User    | string | Tạo JWT access token (2 giờ)          |
| `GenerateRefreshToken()` | -       | string | Tạo refresh token ngẫu nhiên 64 bytes |

**Token Structure (JWT):**

```
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "user-id",
  "email": "user@example.com",
  "role": "Customer",
  "fullName": "User Name",
  "exp": 1234567890
}
Signature: HMACSHA256(base64UrlEncode(header) + "." + base64UrlEncode(payload), secret)
```

#### **c) TokenHashHelper.cs**

**Trách nhiệm:** Hash tokens bằng SHA256 trước lưu vào DB

```csharp
// Tất cả refresh tokens và verification tokens được hash trước khi lưu
// Lý do: Bảo mật, nếu DB bị leak thì tokens vẫn an toàn
TokenHashHelper.Sha256(rawToken) → hash string
```

---

### 3. **CinemaSystem.DAL** (Data Access Layer)

**Mục đích:** Giao tiếp với Database qua Entity Framework Core

**Các thành phần:**

#### **a) CinemaDbContext.cs**

```csharp
public class CinemaDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }
    public DbSet<Movie> Movies { get; set; }
    // ... other entities
}
```

#### **b) Repositories (Interface + Implementation)**

**i) UserRepository.cs - IUserRepository**

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);              // Lấy user theo ID
    Task<User?> GetByEmailAsync(string email);      // Lấy user theo email
    Task CreateAsync(User user);                    // Tạo user mới
    Task UpdateAsync(User user);                    // Cập nhật user
    Task SaveChangesAsync();                        // Lưu thay đổi
}

// Implementation
public class UserRepository : IUserRepository
{
    public async Task CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // Lưu vào DB ngay
    }
}
```

**ii) RefreshTokenRepository.cs - IRefreshTokenRepository**

```csharp
public interface IRefreshTokenRepository
{
    Task CreateAsync(RefreshToken refreshToken);
    Task<RefreshToken?> GetByHashAsync(string tokenHash);           // Tìm token theo hash
    Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId);        // Lấy token active của user
    Task RevokeAsync(RefreshToken token, string? revokedByIp, string? replacedByTokenHash);
    Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp); // Hủy tất cả token của user
    Task SaveChangesAsync();
}
```

**Lý do cần RevokeAllUserTokensAsync:**

- Khi user login, các token cũ bị hủy để ngăn chặn session hijacking
- Nếu refresh token bị reuse, có thể hacker chiếm được token, nên revoke tất cả

**iii) EmailVerificationTokenRepository.cs - IEmailVerificationTokenRepository**

```csharp
public interface IEmailVerificationTokenRepository
{
    Task CreateAsync(EmailVerificationToken token);
    Task<EmailVerificationToken?> GetByHashAsync(string tokenHash);
    Task SaveChangesAsync();
}
```

---

#### **c) Models/Entities**

**i) User.cs - Entity của người dùng**

```csharp
public class User
{
    public Guid Id { get; set; }                    // Primary Key
    public string Email { get; set; }               // Email duy nhất
    public string PasswordHash { get; set; }        // Mật khẩu được hash (BCrypt)
    public string FullName { get; set; }            // Tên đầy đủ
    public string Role { get; set; }                // "Customer", "Admin", "Staff"
    public string Status { get; set; }              // "ACTIVE", "INACTIVE", "BANNED"
    public bool IsEmailVerified { get; set; }       // Đã xác minh email chưa
    public DateTime? LastLogin { get; set; }        // Lần đăng nhập cuối
    public byte FailedLoginCount { get; set; }      // Số lần login sai (dùng cho lock account)
    public DateTime CreatedAt { get; set; }         // Thời gian tạo
    public DateTime UpdatedAt { get; set; }         // Lần cập nhật cuối

    // Navigation Properties
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; }
    public virtual EmailVerificationToken? EmailVerificationToken { get; set; }
}
```

**ii) RefreshToken.cs - Token để làm mới access token**

```csharp
public class RefreshToken
{
    public Guid Id { get; set; }                    // Primary Key
    public Guid UserId { get; set; }                // Foreign Key -> User
    public string TokenHash { get; set; }           // Hash của refresh token (lưu hash không phải raw)
    public DateTime CreatedAt { get; set; }         // Khi token được tạo
    public DateTime ExpiresAt { get; set; }         // Khi token hết hạn (mặc định 30 ngày)
    public string? CreatedByIp { get; set; }        // IP tạo token (dùng cho audit)

    // Revocation info (khi user logout hoặc login lại)
    public bool IsRevoked { get; set; }             // Đã bị revoke chưa
    public DateTime? RevokedAt { get; set; }        // Khi bị revoke
    public string? RevokedByIp { get; set; }        // IP revoke
    public string? ReplacedByToken { get; set; }    // Token thay thế (cho token rotation)

    // Navigation Properties
    public virtual User? User { get; set; }
}
```

**iii) EmailVerificationToken.cs - Token để xác minh email**

```csharp
public class EmailVerificationToken
{
    public Guid Id { get; set; }                    // Primary Key
    public Guid UserId { get; set; }                // Foreign Key -> User
    public string TokenHash { get; set; }           // Hash của verification token
    public DateTime CreatedAt { get; set; }         // Khi token được tạo
    public DateTime ExpiresAt { get; set; }         // Khi token hết hạn (mặc định 30 ngày)

    // Verification info
    public bool IsVerified { get; set; }            // Đã xác minh chưa
    public DateTime? VerifiedAt { get; set; }       // Khi xác minh
    public string? VerifiedByIp { get; set; }       // IP xác minh

    // Navigation Properties
    public virtual User? User { get; set; }
}
```

---

### 4. **CinemaSystem.Common** (Shared/Utilities)

**Mục đích:** Chứa code dùng chung cho tất cả projects

#### **a) DTOs (Data Transfer Objects) - Auth**

| DTO                      | Mục Đích                                                                         |
| ------------------------ | -------------------------------------------------------------------------------- |
| `RegisterRequestDto`     | Nhận dữ liệu từ client khi register (FullName, Email, Password, ConfirmPassword) |
| `RegisterResponseDto`    | Trả về kết quả register (UserId, Email, FullName, VerificationToken)             |
| `LoginRequestDto`        | Nhận email + password từ client                                                  |
| `LoginResponseDto`       | Trả về tokens (AccessToken, RefreshToken, ExpiresAt)                             |
| `RefreshTokenRequestDto` | Nhận refresh token từ client để cấp access token mới                             |
| `VerifyEmailRequestDto`  | Nhận verification token từ client                                                |

#### **b) ApiResponse<T> - Wrapper cho tất cả API responses**

```csharp
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }         // Thành công hay thất bại
    public string Message { get; set; }         // Thông báo cho user
    public T? Data { get; set; }                // Dữ liệu trả về (nếu có)
    public List<string>? Errors { get; set; }   // Chi tiết lỗi validation

    // Helper methods
    public static ApiResponse<T> Success(T? data, string message)
    public static ApiResponse<T> Fail(string message)
    public static ApiResponse<T> Fail(List<string> errors, string message)
}
```

#### **c) Enums**

```csharp
public enum UserRole
{
    Customer = 0,   // Khách hàng
    Admin = 1,      // Quản trị viên
    Staff = 2       // Nhân viên
}

public enum DeleteMovieResult
{
    Success = 0,
    NotFound = 1,
    Error = 2
}
```

#### **d) Services (Interfaces)**

```csharp
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}
```

---

## 🔄 Luồng Chạy REGISTER

### Sơ Đồ Luồng Đăng Ký

```
Client                 API                 Services              DAL/DB
  │                     │                     │                    │
  ├─POST /register      │                     │                    │
  │─ FullName           │                     │                    │
  │─ Email              │                     │                    │
  │─ Password           │                     │                    │
  └────────────────────>│                     │                    │
                        │ Validate request   │                    │
                        │ (FluentValidation) │                    │
                        │                     │                    │
                        │ AuthController     │                    │
                        │ .Register()        │                    │
                        └────────────────────>│ RegisterAsync()    │
                        │                     │ 1. Check email     │
                        │                     │    exists?         │
                        │                     │    ────────────────>│ GetByEmailAsync()
                        │                     │<────────────────────│ Không tìm thấy
                        │                     │ 2. Hash password   │
                        │                     │    (BCrypt)        │
                        │                     │ 3. Create User     │
                        │                     │    ────────────────>│ CreateAsync()
                        │                     │<────────────────────│ ✓ Lưu DB
                        │                     │                    │
                        │                     │ 4. Tạo verification│
                        │                     │    token (raw)     │
                        │                     │ 5. Hash token      │
                        │                     │ 6. Create          │
                        │                     │    EmailVerification│
                        │                     │    Token           │
                        │                     │    ────────────────>│ CreateAsync()
                        │                     │<────────────────────│ ✓ Lưu DB
                        │                     │                    │
                        │                     │ 7. Send email      │
                        │                     │    with link       │
                        │                     │ (Raw token gửi)    │
                        │                     │    ────────────────>│ EmailService
                        │                     │<────────────────────│ ✓ Email sent
                        │                     │                    │
                        │<────────────────────│ Return:            │
                        │ ApiResponse         │ - Success          │
                        │ - Data:             │ - VerificationToken│
                        │   RegisterResponseDto│ (Raw token)        │
  <─────────────────────┤                     │                    │

  Response 201 Created:
  {
    "isSuccess": true,
    "message": "Registration successful. Verify your email before login.",
    "data": {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "fullName": "John Doe",
      "role": "Customer",
      "verificationToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
    }
  }
```

### Chi Tiết Từng Bước

**Bước 1: Client gửi request**

```http
POST /api/auth/register
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john@example.com",
  "password": "Password@123",
  "confirmPassword": "Password@123"
}
```

**Bước 2: AuthController nhận request**

```csharp
[HttpPost("register")]
public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register([FromBody] RegisterRequestDto request)
{
    // FluentValidation tự động validate request
    // Nếu validation fail, controller trả về BadRequest ngay
    var response = await _authService.RegisterAsync(request, GetClientIp());
    return response.IsSuccess ? Created(string.Empty, response) : BadRequest(response);
}
```

**Bước 3: AuthService.RegisterAsync() xử lý logic**

```csharp
public async Task<ApiResponse<RegisterResponseDto>> RegisterAsync(RegisterRequestDto request, string? clientIp = null)
{
    // 3a. Kiểm tra email đã tồn tại chưa
    var existingUser = await _userRepository.GetByEmailAsync(request.Email.Trim());
    if (existingUser is not null)
    {
        return ApiResponse<RegisterResponseDto>.Fail("Email already exists.");
        // → Trả về BadRequest 400 tới client
    }

    // 3b. Tạo user object
    var now = DateTime.UtcNow;
    var user = new User
    {
        Id = Guid.NewGuid(),                           // Tạo ID duy nhất
        Email = request.Email.Trim(),
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),  // Hash password
        FullName = request.FullName.Trim(),
        Role = UserRole.Customer.ToString(),           // Mặc định role Customer
        Status = "ACTIVE",                             // Tài khoản active
        IsEmailVerified = false,                       // Chưa xác minh email
        FailedLoginCount = 0,
        CreatedAt = now,
        UpdatedAt = now
    };

    // 3c. Lưu user vào DB
    await _userRepository.CreateAsync(user);

    // 3d. Tạo verification token (raw)
    var verificationToken = CreateRawToken();  // 64 bytes random base64url
    // Ví dụ: "VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4gSXQgaXMgNjQgYnl0ZXMgbG9uZw=="

    // 3e. Hash token trước lưu vào DB (bảo mật - nếu DB leak, raw token vẫn an toàn)
    var verificationTokenHash = TokenHashHelper.Sha256(verificationToken);
    // SHA256("VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4=")
    // → "a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6"

    // 3f. Lưu EmailVerificationToken vào DB
    await _emailVerificationTokenRepository.CreateAsync(new EmailVerificationToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,                              // Link tới user
        TokenHash = verificationTokenHash,             // Lưu hash, không phải raw
        CreatedAt = now,
        ExpiresAt = now.AddDays(30),                   // Hết hạn trong 30 ngày
        IsVerified = false,
        VerifiedByIp = clientIp
    });

    // 3g. Tạo verification link
    var baseUrl = _configuration["App:BaseUrl"];  // "https://cinema.example.com"
    var verifyLink = $"{baseUrl.TrimEnd('/')}/api/auth/verify-email?token={Uri.EscapeDataString(verificationToken)}";
    // https://cinema.example.com/api/auth/verify-email?token=VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4%3D

    // 3h. Gửi email
    await _emailService.SendEmailAsync(
        user.Email,
        "Verify your email",
        $"<p>Welcome to Cinema System, {user.FullName}.</p><p>Please verify your email by clicking <a href=\"{verifyLink}\">this link</a>.</p>"
    );

    // 3i. Trả về response (gồm raw token để client có thể dùng)
    return ApiResponse<RegisterResponseDto>.Success(
        new RegisterResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            VerificationToken = verificationToken  // Raw token trả về client
        },
        "Registration successful. Verify your email before login."
    );
}
```

**Bước 4: EmailService gửi email**

```csharp
public async Task SendEmailAsync(string to, string subject, string htmlBody)
{
    var smtp = new SmtpSettings();
    _configuration.GetSection("Smtp").Bind(smtp);  // Đọc config SMTP từ appsettings.json

    using var client = new SmtpClient(smtp.Host, smtp.Port)
    {
        EnableSsl = smtp.EnableSsl,
        Credentials = new NetworkCredential(smtp.Username, smtp.Password)
    };

    using var message = new MailMessage();
    message.From = new MailAddress(smtp.FromEmail, smtp.FromName);
    message.To.Add(to);
    message.Subject = subject;
    message.Body = htmlBody;
    message.IsBodyHtml = true;

    await client.SendMailAsync(message);  // Gửi email qua SMTP
}
```

**Bước 5: Client nhận response**

```json
{
  "isSuccess": true,
  "message": "Registration successful. Verify your email before login.",
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "john@example.com",
    "fullName": "John Doe",
    "role": "Customer",
    "verificationToken": "VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4gSXQgaXMgNjQgYnl0ZXMgbG9uZw=="
  }
}
```

---

## 🔑 Luồng Chạy LOGIN

### Sơ Đồ Luồng Đăng Nhập

```
Client                 API                 Services              DAL/DB
  │                     │                     │                    │
  ├─POST /login         │                     │                    │
  │─ Email              │                     │                    │
  │─ Password           │                     │                    │
  └────────────────────>│                     │                    │
                        │ Validate request   │                    │
                        │                     │                    │
                        │ AuthController     │                    │
                        │ .Login()           │                    │
                        └────────────────────>│ LoginAsync()       │
                        │                     │ 1. Find user by    │
                        │                     │    email           │
                        │                     │    ────────────────>│ GetByEmailAsync()
                        │                     │<────────────────────│ Tìm thấy user
                        │                     │                    │
                        │                     │ 2. Check status    │
                        │                     │    (ACTIVE?)       │
                        │                     │                    │
                        │                     │ 3. Check email     │
                        │                     │    verified?       │
                        │                     │                    │
                        │                     │ 4. Verify password │
                        │                     │    (BCrypt)        │
                        │                     │                    │
                        │                     │ 5. Reset           │
                        │                     │    failedLoginCount│
                        │                     │    Update LastLogin│
                        │                     │    ────────────────>│ UpdateAsync()
                        │                     │<────────────────────│ ✓ Updated
                        │                     │                    │
                        │                     │ 6. Revoke old      │
                        │                     │    refresh tokens  │
                        │                     │    ────────────────>│ RevokeAllUserTokensAsync()
                        │                     │<────────────────────│ ✓ Revoked
                        │                     │                    │
                        │                     │ 7. Create token    │
                        │                     │    pair:           │
                        │                     │    - AccessToken   │
                        │                     │    - RefreshToken  │
                        │                     │    ────────────────>│ CreateAsync()
                        │                     │<────────────────────│ ✓ Saved
                        │                     │                    │
                        │<────────────────────│ Return tokens      │
                        │ ApiResponse         │                    │
  <─────────────────────┤                     │                    │

  Response 200 OK:
  {
    "isSuccess": true,
    "message": "Login successful.",
    "data": {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "fullName": "John Doe",
      "role": "Customer",
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "refreshToken": "VGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4uIEl0IGlzIDY0IGJ5dGVzIGxvbmcgYW5kIHJhbmRvbQ==",
      "accessTokenExpiresAt": "2024-12-17T10:30:00Z"
    }
  }
```

### Chi Tiết Từng Bước

**Bước 1: Client gửi email + password**

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "john@example.com",
  "password": "Password@123"
}
```

**Bước 2: AuthService.LoginAsync() kiểm tra credentials**

```csharp
public async Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request, string? clientIp = null)
{
    // 2a. Tìm user từ email
    var user = await _userRepository.GetByEmailAsync(request.Email.Trim());
    if (user is null)
    {
        return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");
        // ⚠️ Thông báo chung cho cả email không tồn tại hoặc password sai (bảo mật)
    }

    // 2b. Kiểm tra status
    if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase))
    {
        return ApiResponse<LoginResponseDto>.Fail("User account is inactive.");
    }

    // 2c. Kiểm tra email đã verified chưa
    if (!user.IsEmailVerified)
    {
        return ApiResponse<LoginResponseDto>.Fail("Email has not been verified.");
    }

    // 2d. Verify password
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        // Password sai → Tăng failed login count (dùng cho account lock)
        user.FailedLoginCount = (byte)Math.Min(user.FailedLoginCount + 1, byte.MaxValue);
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
        return ApiResponse<LoginResponseDto>.Fail("Invalid email or password.");
    }

    // 2e. Password đúng → Reset counter, update last login
    user.FailedLoginCount = 0;
    user.LastLogin = DateTime.UtcNow;
    user.UpdatedAt = DateTime.UtcNow;
    await _userRepository.UpdateAsync(user);

    // 2f. Hủy tất cả refresh token cũ (security: ngăn session reuse)
    await _refreshTokenRepository.RevokeAllUserTokensAsync(user.Id, clientIp);

    // 2g. Tạo token pair mới (access + refresh)
    var loginPair = await CreateTokenPairAsync(user, clientIp);
    return ApiResponse<LoginResponseDto>.Success(loginPair, "Login successful.");
}
```

**Bước 3: CreateTokenPairAsync() tạo token**

```csharp
private async Task<LoginResponseDto> CreateTokenPairAsync(User user, string? clientIp)
{
    // 3a. Tạo JWT access token
    var accessToken = _jwtService.GenerateAccessToken(user);
    // Nội dung:
    // {
    //   "sub": "550e8400-e29b-41d4-a716-446655440000",
    //   "email": "john@example.com",
    //   "role": "Customer",
    //   "fullName": "John Doe",
    //   "exp": 1734416400,
    //   "iat": 1734412800,
    //   "iss": "CinemaSystem",
    //   "aud": "CinemaSystem"
    // }

    var accessTokenExpiresAt = DateTime.UtcNow.AddHours(GetAccessTokenLifetimeHours()); // +2 giờ

    // 3b. Tạo refresh token (random 64 bytes)
    var refreshToken = _jwtService.GenerateRefreshToken();
    // Ví dụ: "VGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4uIEl0IGlzIDY0IGJ5dGVzIGxvbmcgYW5kIHJhbmRvbQ=="

    var refreshTokenHash = TokenHashHelper.Sha256(refreshToken);
    // SHA256 của refresh token

    var refreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()); // +30 ngày

    // 3c. Lưu refresh token vào DB (lưu hash, không phải raw)
    await _refreshTokenRepository.CreateAsync(new RefreshToken
    {
        Id = Guid.NewGuid(),
        UserId = user.Id,
        TokenHash = refreshTokenHash,              // Lưu hash (bảo mật)
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = refreshTokenExpiresAt,
        CreatedByIp = clientIp,
        IsRevoked = false
    });

    // 3d. Trả về token pair
    return new LoginResponseDto
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = NormalizeRole(user.Role),
        AccessToken = accessToken,                 // JWT string
        RefreshToken = refreshToken,               // Raw token (64 bytes base64url)
        AccessTokenExpiresAt = accessTokenExpiresAt
    };
}
```

**Bước 4: Client nhận tokens**

```json
{
  "isSuccess": true,
  "message": "Login successful.",
  "data": {
    "userId": "550e8400-e29b-41d4-a716-446655440000",
    "email": "john@example.com",
    "fullName": "John Doe",
    "role": "Customer",
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI1NTBlODQwMC1lMjliLTQxZDQtYTcxNi00NDY2NTU0NDAwMDAiLCJlbWFpbCI6ImpvaG5AZXhhbXBsZS5jb20iLCJyb2xlIjoiQ3VzdG9tZXIiLCJmdWxsTmFtZSI6IkpvaG4gRG9lIiwiZXhwIjoxNzM0NDE2NDAwLCJpYXQiOjE3MzQ0MTI4MDAsImlzcyI6IkNpbmVtYVN5c3RlbSIsImF1ZCI6IkNpbmVtYVN5c3RlbSJ9.QrHzLfO6TXzY7oY5oYxXyxYzZaBbCdDeEeFfGgHhIjI",
    "refreshToken": "VGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4uIEl0IGlzIDY0IGJ5dGVzIGxvbmcgYW5kIHJhbmRvbQ==",
    "accessTokenExpiresAt": "2024-12-17T10:30:00Z"
  }
}
```

**Bước 5: Client dùng access token để call API khác**

```http
GET /api/movies
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

Middleware xác thực JWT:

- Extract JWT từ Authorization header
- Verify signature (dùng secret key)
- Kiểm tra expiration time
- Tạo Claims từ payload (sub, email, role, fullName)
- Cho phép request tiếp tục

---

## ✉️ Luồng Chạy EMAIL VERIFICATION

### Sơ Đồ Luồng Xác Minh Email

```
Sau khi Register, user nhận email với link:
https://cinema.example.com/api/auth/verify-email?token=VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4%3D

Client click link hoặc POST token

Client                 API                 Services              DAL/DB
  │                     │                     │                    │
  ├─GET /verify-email   │                     │                    │
  │  ?token=...         │                     │                    │
  └────────────────────>│                     │                    │
                        │ Extract token param│                    │
                        │                     │                    │
                        │ AuthController     │                    │
                        │ .VerifyEmailGet() │                    │
                        │ → .VerifyEmail()   │                    │
                        └────────────────────>│ VerifyEmailAsync() │
                        │                     │ 1. Hash token      │
                        │                     │    ────────────────>│ GetByHashAsync()
                        │                     │<────────────────────│ Token found
                        │                     │                    │
                        │                     │ 2. Check:          │
                        │                     │    - Already       │
                        │                     │      verified?     │
                        │                     │    - Expired?      │
                        │                     │                    │
                        │                     │ 3. Mark token as   │
                        │                     │    verified        │
                        │                     │    ────────────────>│ SaveChangesAsync()
                        │                     │<────────────────────│ ✓ Updated
                        │                     │                    │
                        │                     │ 4. Mark user's     │
                        │                     │    email verified   │
                        │                     │    ────────────────>│ UpdateAsync()
                        │                     │<────────────────────│ ✓ Updated
                        │                     │                    │
                        │<────────────────────│ Return success      │
                        │ ApiResponse         │                    │
  <─────────────────────┤                     │                    │

  Response 200 OK:
  {
    "isSuccess": true,
    "message": "Email verified successfully.",
    "data": "Email verified."
  }
```

### Chi Tiết Từng Bước

**Bước 1: User click email verification link**

```
Link trong email:
https://cinema.example.com/api/auth/verify-email?token=VGhpcyBpcyBhIHZlcmlmaWNhdGlvbiB0b2tlbi4gSXQgaXMgNjQgYnl0ZXMgbG9uZw==
```

**Bước 2: AuthController nhận GET request**

```csharp
[HttpGet("verify-email")]
public async Task<ActionResult<ApiResponse<object?>>> VerifyEmailGet([FromQuery] string token)
{
    var dto = new VerifyEmailRequestDto { Token = token };
    var response = await _authService.VerifyEmailAsync(dto, GetClientIp());
    return response.IsSuccess ? Ok(response) : BadRequest(response);
}
```

**Bước 3: AuthService.VerifyEmailAsync() xác minh**

```csharp
public async Task<ApiResponse<string>> VerifyEmailAsync(VerifyEmailRequestDto request, string? clientIp = null)
{
    // 3a. Kiểm tra token có được truyền vào
    if (string.IsNullOrWhiteSpace(request.Token))
    {
        return ApiResponse<string>.Fail("Verification token is required.");
    }

    var incoming = request.Token.Trim();

    // 3b. Hash token (để so sánh với hash trong DB)
    var tokenHash = TokenHashHelper.Sha256(incoming);

    // 3c. Tìm email verification token trong DB
    var verificationToken = await _emailVerificationTokenRepository.GetByHashAsync(tokenHash);
    if (verificationToken is null)
    {
        return ApiResponse<string>.Fail("Invalid verification token.");
    }

    // 3d. Kiểm tra token đã verified chưa
    if (verificationToken.IsVerified)
    {
        return ApiResponse<string>.Success("Email already verified.", "Email verified successfully.");
        // ℹ️ Trả về success nếu đã verify, không lỗi (user-friendly)
    }

    // 3e. Kiểm tra token đã hết hạn chưa
    if (verificationToken.ExpiresAt <= DateTime.UtcNow)
    {
        return ApiResponse<string>.Fail("Verification token expired.");
    }

    // 3f. Mark token as verified
    verificationToken.IsVerified = true;
    verificationToken.VerifiedAt = DateTime.UtcNow;
    verificationToken.VerifiedByIp = clientIp;
    await _emailVerificationTokenRepository.SaveChangesAsync();

    // 3g. Update user - mark email as verified
    var user = verificationToken.User ?? await _userRepository.GetByIdAsync(verificationToken.UserId);
    if (user is not null)
    {
        user.IsEmailVerified = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user);
    }

    return ApiResponse<string>.Success("Email verified.", "Email verified successfully.");
}
```

**Bước 4: User có thể login**

- Bây giờ `user.IsEmailVerified = true`
- Lần tới user login, kiểm tra `if (!user.IsEmailVerified)` sẽ pass

---

## 🔄 Luồng Chạy REFRESH TOKEN

### Sơ Đồ Luồng Làm Mới Token

```
Khi access token sắp hết hạn (hoặc hết hạn), client dùng refresh token để cấp access token mới

Client                 API                 Services              DAL/DB
  │                     │                     │                    │
  ├─POST /refresh-token │                     │                    │
  │  - refreshToken: "|                     │                    │
  └────────────────────>│                     │                    │
                        │ Validate request   │                    │
                        │                     │                    │
                        │ AuthController     │                    │
                        │ .RefreshToken()    │                    │
                        └────────────────────>│ RefreshTokenAsync() │
                        │                     │ 1. Hash refresh    │
                        │                     │    token           │
                        │                     │    ────────────────>│ GetByHashAsync()
                        │                     │<────────────────────│ Token found
                        │                     │                    │
                        │                     │ 2. Check:          │
                        │                     │    - Revoked?      │
                        │                     │    - Expired?      │
                        │                     │                    │
                        │ IF REUSE DETECTED: │                    │
                        │ (Revoked + used)   │                    │
                        │ → Revoke all user  │                    │
                        │    tokens          │                    │
                        │    ────────────────>│ RevokeAllUserTokens
                        │<────────────────────│ ✓ Revoked
                        │ Return error       │                    │
                        │                    │                    │
                        │ IF VALID:          │                    │
                        │ 3. Rotate token:   │                    │
                        │    - Revoke old    │                    │
                        │    - Create new    │                    │
                        │    ────────────────>│ RevokeAsync()
                        │<────────────────────│ ✓ Revoked
                        │                    │                    │
                        │                    │ CreateAsync()
                        │<────────────────────│ ✓ Created
                        │                    │                    │
                        │<────────────────────│ Return new tokens  │
                        │ ApiResponse         │                    │
  <─────────────────────┤                     │                    │

  Response 200 OK:
  {
    "isSuccess": true,
    "message": "Token refreshed successfully.",
    "data": {
      "userId": "550e8400-e29b-41d4-a716-446655440000",
      "email": "user@example.com",
      "fullName": "John Doe",
      "role": "Customer",
      "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (NEW)",
      "refreshToken": "VGhpcyBpcyBhIG5ldyByZWZyZXNoIHRva2VuLi4u (NEW)",
      "accessTokenExpiresAt": "2024-12-17T11:30:00Z"
    }
  }
```

### Chi Tiết Từng Bước

**Bước 1: Client gửi refresh token**

```http
POST /api/auth/refresh-token
Content-Type: application/json

{
  "refreshToken": "VGhpcyBpcyBhIHJlZnJlc2ggdG9rZW4uIEl0IGlzIDY0IGJ5dGVzIGxvbmcgYW5kIHJhbmRvbQ=="
}
```

**Bước 2: AuthService.RefreshTokenAsync() xử lý**

```csharp
public async Task<ApiResponse<LoginResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto request, string? clientIp = null)
{
    // 2a. Kiểm tra refresh token được truyền vào
    if (string.IsNullOrWhiteSpace(request.RefreshToken))
    {
        return ApiResponse<LoginResponseDto>.Fail("Refresh token is required.");
    }

    // 2b. Hash token để so sánh trong DB
    var tokenHash = TokenHashHelper.Sha256(request.RefreshToken.Trim());

    // 2c. Tìm refresh token trong DB
    var refreshToken = await _refreshTokenRepository.GetByHashAsync(tokenHash);
    if (refreshToken is null)
    {
        return ApiResponse<LoginResponseDto>.Fail("Invalid refresh token.");
    }

    // 2d. Kiểm tra token đã bị revoke chưa
    if (refreshToken.IsRevoked)
    {
        // ⚠️ SECURITY ALERT: Token bị revoke nhưng bị dùng lại → Dấu hiệu tấn công!
        // Revoke tất cả token của user để buộc re-login
        await _refreshTokenRepository.RevokeAllUserTokensAsync(refreshToken.UserId, clientIp);
        return ApiResponse<LoginResponseDto>.Fail("Refresh token reuse detected. Please login again.");
    }

    // 2e. Kiểm tra token hết hạn
    if (refreshToken.ExpiresAt <= DateTime.UtcNow)
    {
        await _refreshTokenRepository.RevokeAsync(refreshToken, clientIp, tokenHash);
        return ApiResponse<LoginResponseDto>.Fail("Refresh token expired.");
    }

    // 2f. Lấy user (từ lazy loading hoặc query)
    var user = refreshToken.User ?? await _userRepository.GetByIdAsync(refreshToken.UserId);
    if (user is null)
    {
        return ApiResponse<LoginResponseDto>.Fail("Invalid refresh token.");
    }

    // 2g. Kiểm tra user account còn active và email verified
    if (!string.Equals(user.Status, "ACTIVE", StringComparison.OrdinalIgnoreCase) || !user.IsEmailVerified)
    {
        return ApiResponse<LoginResponseDto>.Fail("User account is not allowed to refresh tokens.");
    }

    // 2h. Token pair rotation (revoke cũ, tạo mới)
    var loginPair = await RotateTokenPairAsync(user, refreshToken, clientIp);
    return ApiResponse<LoginResponseDto>.Success(loginPair, "Token refreshed successfully.");
}
```

**Bước 3: RotateTokenPairAsync() xoay token**

```csharp
private async Task<LoginResponseDto> RotateTokenPairAsync(User user, RefreshToken currentToken, string? clientIp)
{
    // 3a. Tạo access token mới
    var accessToken = _jwtService.GenerateAccessToken(user);
    var accessTokenExpiresAt = DateTime.UtcNow.AddHours(GetAccessTokenLifetimeHours()); // +2 giờ

    // 3b. Tạo refresh token mới (64 bytes random)
    var newRefreshToken = _jwtService.GenerateRefreshToken();
    var newRefreshTokenHash = TokenHashHelper.Sha256(newRefreshToken);
    var newRefreshTokenExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenLifetimeDays()); // +30 ngày

    // 3c. Revoke old refresh token (đánh dấu là revoked, link đến token mới)
    await _refreshTokenRepository.RevokeAsync(
        currentToken,                 // Token cũ
        clientIp,                     // Revoked by IP
        newRefreshTokenHash           // Replaced by token (link)
    );

    // 3d. Tạo refresh token mới
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

    // 3e. Trả về token pair mới
    return new LoginResponseDto
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.FullName,
        Role = NormalizeRole(user.Role),
        AccessToken = accessToken,          // Access token mới
        RefreshToken = newRefreshToken,     // Refresh token mới
        AccessTokenExpiresAt = accessTokenExpiresAt
    };
}
```

**Bước 4: Client nhận tokens mới**

- Access token mới được cấp
- Refresh token mới được tạo
- Token cũ bị revoke (không dùng lại được)

---

## 📚 Chi Tiết Các Lớp và Phương Thức

### AuthController.cs

```csharp
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    // Constructor Injection - Dependency được inject tự động
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// POST /api/auth/register
    /// Client gửi thông tin đăng ký, server tạo user và gửi email verify
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<RegisterResponseDto>>> Register(
        [FromBody] RegisterRequestDto request)
    {
        // GetClientIp() lấy IP của client từ HttpContext
        // Dùng cho audit log (biết request từ IP nào)
        var response = await _authService.RegisterAsync(request, GetClientIp());

        // Nếu success, trả về 201 Created; nếu fail, trả về 400 BadRequest
        return response.IsSuccess ? Created(string.Empty, response) : BadRequest(response);
    }

    /// <summary>
    /// POST /api/auth/login
    /// Client gửi email + password, server xác thực và cấp tokens
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        [FromBody] LoginRequestDto request)
    {
        var response = await _authService.LoginAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : Unauthorized(response); // 401 nếu fail
    }

    /// <summary>
    /// POST /api/auth/refresh-token
    /// Client gửi refresh token, server cấp access token mới
    /// </summary>
    [HttpPost("refresh-token")]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> RefreshToken(
        [FromBody] RefreshTokenRequestDto request)
    {
        var response = await _authService.RefreshTokenAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : Unauthorized(response);
    }

    /// <summary>
    /// POST /api/auth/verify-email
    /// Client gửi token qua body
    /// </summary>
    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<object?>>> VerifyEmail(
        [FromBody] VerifyEmailRequestDto request)
    {
        var response = await _authService.VerifyEmailAsync(request, GetClientIp());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// GET /api/auth/verify-email?token=...
    /// Client click email link, GET endpoint gọi VerifyEmail
    /// </summary>
    [HttpGet("verify-email")]
    public async Task<ActionResult<ApiResponse<object?>>> VerifyEmailGet([FromQuery] string token)
    {
        var dto = new VerifyEmailRequestDto { Token = token };
        var response = await _authService.VerifyEmailAsync(dto, GetClientIp());
        return response.IsSuccess ? Ok(response) : BadRequest(response);
    }

    /// <summary>
    /// Helper: Lấy IP của client
    /// Dùng cho audit log và security (track requests từ IP nào)
    /// </summary>
    private string? GetClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString();
}
```

### JwtService.cs

```csharp
/// <summary>
/// Service tạo JWT access tokens và refresh tokens
/// Không liên quan đến DB, chỉ tạo tokens
/// </summary>
public sealed class JwtService : IJwtService
{
    private readonly IConfiguration _configuration;

    public JwtService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Tạo JWT access token (mặc định 2 giờ hạn)
    ///
    /// Claims được thêm:
    /// - sub (subject): User ID
    /// - email: Email của user
    /// - role: Role của user (Customer, Admin, Staff)
    /// - fullName: Tên đầy đủ
    ///
    /// Token được sign bằng HS256 (HMAC SHA256) với secret key từ config
    /// </summary>
    public string GenerateAccessToken(User user)
    {
        var key = _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "CinemaSystem";
        var audience = _configuration["Jwt:Audience"] ?? "CinemaSystem";
        var lifetimeHours = int.TryParse(_configuration["Jwt:AccessTokenExpirationHours"], out var hours) ? hours : 2;

        //var expiresAt = DateTime.UtcNow.AddHours(lifetimeHours);
        var expiresAt = now.AddMinutes(1);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, NormalizeRole(user.Role)),
            new("fullName", user.FullName)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token); // Encode token thành string
    }

    /// <summary>
    /// Tạo refresh token (64 bytes random, base64url encoded)
    ///
    /// Refresh token:
    /// - Không có structure (không phải JWT)
    /// - Chỉ là string ngẫu nhiên 64 bytes
    /// - Không có expiration info trong token (phải lưu trong DB)
    /// - Dùng để cấp access token mới khi hết hạn
    /// </summary>
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64); // 64 bytes random
        return Base64UrlEncode(bytes);                  // Encode base64url
    }

    /// <summary>
    /// Base64Url encoding (URL-safe)
    /// - Replace '+' với '-'
    /// - Replace '/' với '_'
    /// - Trim '=' padding
    ///
    /// Lý do: URL-safe, có thể truyền qua URL query string
    /// </summary>
    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Chuẩn hóa role (parse từ string)
    /// Nếu role không hợp lệ, mặc định là Customer
    /// </summary>
    private static string NormalizeRole(string role)
    {
        return Enum.TryParse<UserRole>(role, true, out var parsedRole)
            ? parsedRole.ToString()
            : UserRole.Customer.ToString();
    }
}
```

### TokenHashHelper.cs

```csharp
/// <summary>
/// Utility để hash tokens trước khi lưu vào DB
///
/// Lý do hash:
/// - Bảo mật: Nếu DB bị leak, raw tokens không bị expose
/// - Giống cách hash password (bcrypt)
/// - Khi client gửi token, ta hash nó rồi so sánh với hash trong DB
/// </summary>
public static class TokenHashHelper
{
    /// <summary>
    /// Hash token bằng SHA256
    /// </summary>
    public static string Sha256(string input)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes); // Hex string
        }
    }
}
```

### EmailService.cs

```csharp
/// <summary>
/// Service gửi email qua SMTP
///
/// Config từ appsettings.json:
/// {
///   "Smtp": {
///     "Host": "smtp.gmail.com",
///     "Port": 587,
///     "Username": "your-email@gmail.com",
///     "Password": "app-password",
///     "EnableSsl": true,
///     "FromEmail": "noreply@cinema.com",
///     "FromName": "Cinema System"
///   }
/// }
/// </summary>
public sealed class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Gửi email
    /// </summary>
    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        // Đọc SMTP config từ appsettings.json
        var smtp = new SmtpSettings();
        _configuration.GetSection("Smtp").Bind(smtp);

        if (string.IsNullOrWhiteSpace(smtp.Host) || smtp.Port == 0)
        {
            _logger.LogError("SMTP configuration is missing or incomplete.");
            throw new InvalidOperationException("SMTP configuration is missing or incomplete.");
        }

        // Tạo email message
        using var message = new MailMessage();
        message.From = new MailAddress(smtp.FromEmail ?? smtp.Username ?? "no-reply@example.com", smtp.FromName ?? "Cinema System");
        message.To.Add(to);
        message.Subject = subject;
        message.Body = htmlBody;
        message.IsBodyHtml = true; // HTML email

        // Tạo SMTP client và gửi
        using var client = new SmtpClient(smtp.Host, smtp.Port)
        {
            EnableSsl = smtp.EnableSsl,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password)
        };

        _logger.LogInformation("Sending email to {Email} via SMTP {Host}:{Port}", to, smtp.Host, smtp.Port);
        await client.SendMailAsync(message);
        _logger.LogInformation("Email sent to {Email}", to);
    }
}

/// <summary>
/// SMTP config class (dùng cho Bind từ appsettings.json)
/// </summary>
internal sealed class SmtpSettings
{
    public string? Host { get; set; }
    public int Port { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool EnableSsl { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
}
```

### Repository Implementations

**UserRepository.cs**

```csharp
/// <summary>
/// Repository cho User entity
/// Chứa CRUD operations cho User
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly CinemaDbContext _context;

    public UserRepository(CinemaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy user theo ID
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FirstOrDefaultAsync(user => user.Id == id);
    }

    /// <summary>
    /// Lấy user theo email
    /// Dùng trong login/register để check email tồn tại
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Tạo user mới
    /// </summary>
    public async Task CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync(); // Lưu vào DB ngay
    }

    /// <summary>
    /// Cập nhật user
    /// </summary>
    public async Task UpdateAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Lưu tất cả changes (dùng khi gọi SaveChanges từ ngoài)
    /// </summary>
    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
```

**RefreshTokenRepository.cs**

```csharp
/// <summary>
/// Repository cho RefreshToken entity
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CinemaDbContext _context;

    public RefreshTokenRepository(CinemaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo refresh token mới
    /// </summary>
    public async Task CreateAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tìm token theo hash
    /// Khi client gửi token, ta hash nó rồi tìm trong DB
    /// </summary>
    public async Task<RefreshToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.RefreshTokens
            .Include(token => token.User)  // Eager load User (để không N+1 query)
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    /// <summary>
    /// Lấy token active của user
    /// Dùng để check xem user có token valid không
    /// </summary>
    public async Task<RefreshToken?> GetActiveByUserIdAsync(Guid userId)
    {
        return await _context.RefreshTokens
            .FirstOrDefaultAsync(token =>
                token.UserId == userId &&
                !token.IsRevoked &&
                token.ExpiresAt > DateTime.UtcNow);
    }

    /// <summary>
    /// Revoke một token (đánh dấu revoked)
    /// Dùng khi user logout hoặc token được rotate
    /// </summary>
    public async Task RevokeAsync(
        RefreshToken refreshToken,
        string? revokedByIp,
        string? replacedByTokenHash = null)
    {
        refreshToken.IsRevoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = revokedByIp;
        refreshToken.ReplacedByToken = replacedByTokenHash; // Link đến token thay thế (audit trail)
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Revoke tất cả token của user
    /// Dùng khi:
    /// - User login (clear old sessions)
    /// - User logout
    /// - Token reuse detected (security incident)
    /// </summary>
    public async Task RevokeAllUserTokensAsync(Guid userId, string? revokedByIp = null)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(token => token.UserId == userId && !token.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            token.RevokedByIp = revokedByIp;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Lưu changes
    /// </summary>
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
```

**EmailVerificationTokenRepository.cs**

```csharp
/// <summary>
/// Repository cho EmailVerificationToken
/// </summary>
public class EmailVerificationTokenRepository : IEmailVerificationTokenRepository
{
    private readonly CinemaDbContext _context;

    public EmailVerificationTokenRepository(CinemaDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo verification token mới
    /// </summary>
    public async Task CreateAsync(EmailVerificationToken token)
    {
        _context.EmailVerificationTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Tìm verification token theo hash
    /// </summary>
    public async Task<EmailVerificationToken?> GetByHashAsync(string tokenHash)
    {
        return await _context.EmailVerificationTokens
            .Include(token => token.User)  // Eager load User
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash);
    }

    /// <summary>
    /// Lưu changes
    /// </summary>
    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
```

---

## 🔐 Security Considerations (Bảo Mật)

### 1. Password Hashing

- ✅ Dùng **BCrypt** để hash password (không bao giờ lưu raw password)
- ✅ BCrypt tự động salt + mở rộng hashing iterations
- ✅ Khi verify, dùng `BCrypt.Verify()` không phải so sánh string

### 2. Token Storage

- ✅ **JWT Access Token**: Không hash (dùng để parse claims)
- ✅ **Refresh Token**: Hash trước lưu (bảo mật - nếu DB leak, raw token không bị expose)
- ✅ **Verification Token**: Hash trước lưu

### 3. Token Rotation

- ✅ Khi refresh, token cũ bị revoke ngay
- ✅ Nếu phát hiện reuse token (revoked + dùng lại), revoke tất cả → buộc re-login
- ✅ Tracking: `ReplacedByToken` field để audit trail

### 4. IP Tracking

- ✅ Lưu IP tạo/revoke token (dùng cho forensics)
- ✅ Giúp phát hiện unauthorized access

### 5. Failed Login Attempts

- ✅ Tăng `FailedLoginCount` mỗi lần login sai
- ✅ Có thể implement account lock nếu count > threshold

### 6. Email Verification

- ✅ Token có expiration (30 ngày)
- ✅ Token có thể dùng một lần (IsVerified = true)
- ✅ User không thể login cho đến khi email verified

---

## 📝 Database Schema (SQL)

```sql
-- User table
CREATE TABLE Users (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    Email NVARCHAR(256) UNIQUE NOT NULL,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    FullName NVARCHAR(256) NOT NULL,
    Phone NVARCHAR(20),
    AvatarUrl NVARCHAR(MAX),
    Role NVARCHAR(50) NOT NULL,               -- "Customer", "Admin", "Staff"
    Status NVARCHAR(50) NOT NULL,             -- "ACTIVE", "INACTIVE", "BANNED"
    IsEmailVerified BIT NOT NULL,             -- 1 = verified
    LastLogin DATETIME2,
    FailedLoginCount TINYINT NOT NULL,        -- 0-255
    LockedUntil DATETIME2,
    CreatedAt DATETIME2 NOT NULL,
    UpdatedAt DATETIME2 NOT NULL
);

-- RefreshToken table
CREATE TABLE RefreshTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL REFERENCES Users(Id),
    TokenHash NVARCHAR(MAX) NOT NULL,         -- SHA256 hash, không phải raw token
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    CreatedByIp NVARCHAR(50),
    IsRevoked BIT NOT NULL,
    RevokedAt DATETIME2,
    RevokedByIp NVARCHAR(50),
    ReplacedByToken NVARCHAR(MAX),            -- Link đến token thay thế
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- EmailVerificationToken table
CREATE TABLE EmailVerificationTokens (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE REFERENCES Users(Id),
    TokenHash NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    ExpiresAt DATETIME2 NOT NULL,
    IsVerified BIT NOT NULL,
    VerifiedAt DATETIME2,
    VerifiedByIp NVARCHAR(50),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);
```

---

## 🎯 Tóm Tắt Luồng Chính

| Luồng             | Endpoint               | Mô Tả                                 | Kết Quả                                     |
| ----------------- | ---------------------- | ------------------------------------- | ------------------------------------------- |
| **Register**      | POST /register         | Tạo user, gửi email verify            | 201 Created + VerificationToken             |
| **Verify Email**  | GET/POST /verify-email | Xác minh email từ token               | 200 OK (email verified)                     |
| **Login**         | POST /login            | Xác thực email+password, cấp tokens   | 200 OK + AccessToken + RefreshToken         |
| **Refresh Token** | POST /refresh-token    | Cấp access token mới từ refresh token | 200 OK + AccessToken mới + RefreshToken mới |

---

## 📞 Các Config Cần Trong appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=CinemaDb;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-characters-long-for-HS256",
    "Issuer": "CinemaSystem",
    "Audience": "CinemaSystem",
    "AccessTokenExpirationHours": 2,
    "RefreshTokenExpirationDays": 30
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "your-email@gmail.com",
    "Password": "your-app-password",
    "EnableSsl": true,
    "FromEmail": "noreply@cinema.com",
    "FromName": "Cinema System"
  },
  "App": {
    "BaseUrl": "https://cinema.example.com"
  }
}
```

---

Đây là tài liệu chi tiết về luồng chạy của hệ thống. Nếu bạn có câu hỏi về bất kỳ phần nào, hãy cho tôi biết! 🚀
