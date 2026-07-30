# Cinema Booking System - System Overview

> **Version:** 1.0.0  
> **Last Updated:** July 2026  
> **Status:** Production Ready

---

## 1. Executive Summary

**Cinema Booking System** là hệ thống quản lý rạp chiếu phí toàn diện, hỗ trợ đặt vé trực tuyến, quản lý phòng chiếu, bán hàng F&B, và tích hợp thanh toán điện tử. Hệ thống được thiết kế theo kiến trúc microservices-ready với RESTful API, đảm bảo khả năng mở rộng và bảo mật cao.

### Key Highlights

| Metric | Value |
|--------|-------|
| **API Endpoints** | 50+ endpoints |
| **Authentication** | JWT + Refresh Token |
| **Payment Gateway** | VNPay tích hợp |
| **User Roles** | 4 vai trò (Admin, Staff, Customer, Guest) |
| **Technology** | .NET 10 / Entity Framework Core 10 |
| **Database** | SQL Server on Azure |

---

## 2. System Architecture

### 2.1 Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              CLIENT LAYER                                    │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐ │
│  │   Web Frontend   │  │  Mobile App     │  │    POS (Point of Sale)      │ │
│  │  (React/Vite)    │  │  (React Native) │  │    (Staff Terminal)        │ │
│  └────────┬────────┘  └────────┬────────┘  └──────────────┬──────────────┘ │
└───────────┼─────────────────────┼──────────────────────────┼────────────────┘
            │                     │                          │
            └─────────────────────┼──────────────────────────┘
                                  │ HTTPS
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                              API GATEWAY                                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                     CinemaSystem.API (.NET 10)                          ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌────────────────┐ ││
│  │  │ Rate Limiter│  │  CORS Policy │  │ JWT Auth    │  │ Global Error   │ ││
│  │  │ (IP-based)  │  │  (Config)    │  │ Middleware  │  │ Handler        │ ││
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └────────────────┘ ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                  │
            ┌─────────────────────┼─────────────────────┐
            ▼                     ▼                     ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           SERVICE LAYER                                       │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ AuthService   │  │ BookingService│  │ PaymentService│  │ Showtime     │  │
│  │ - Login       │  │ - Create      │  │ - VNPay       │  │ Service      │  │
│  │ - Register    │  │ - Cancel      │  │ - IPN Handler │  │ - Schedule   │  │
│  │ - JWT/Refresh │  │ - Get Seats   │  │ - Refund      │  │ - Overlap    │  │
│  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────┐  ┌──────────────┐  │
│  │ MovieService  │  │ FnbService    │  │ QrTicket      │  │ Promotion   │  │
│  │ - CRUD Movie  │  │ - Order F&B   │  │ Service       │  │ Service      │  │
│  │ - Categories  │  │ - Cart        │  │ - Generate QR │  │ - Codes      │  │
│  └───────────────┘  └───────────────┘  └───────────────┘  └──────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DATA ACCESS LAYER                                     │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    Repository Pattern + Unit of Work                     ││
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  ││
│  │  │ User Repo   │  │ Booking Repo│  │ Showtime    │  │ Payment Repo    │  ││
│  │  │ Seat Repo   │  │ Movie Repo  │  │ Theater Repo│  │ FnbOrder Repo   │  ││
│  │  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────────┘  ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                  │
                                  ▼
                    ┌─────────────────────────────┐
                    │   SQL Server (Azure)        │
                    │   - ACID Transactions       │
                    │   - Soft Delete             │
                    │   - Optimistic Concurrency  │
                    └─────────────────────────────┘
```

### 2.2 Layer Responsibilities

| Layer | Responsibility | Key Technologies |
|-------|---------------|-----------------|
| **API** | HTTP handling, auth, validation, rate limiting | ASP.NET Core 10, JWT Bearer, FluentValidation |
| **Services** | Business logic, orchestration | .NET 10, Business Rules Engine |
| **DAL** | Data access, repository pattern | Entity Framework Core 10 |
| **Common** | Shared models, constants, utilities | DTOs, Enums, Exceptions |

---

## 3. Technology Stack

### Backend
```
Framework       : ASP.NET Core 10.0
ORM             : Entity Framework Core 10.0
Database        : SQL Server (Azure)
Authentication  : JWT Bearer + Refresh Token Rotation
Password Hash   : BCrypt
Validation      : FluentValidation
API Docs        : OpenAPI / Swagger
QR Generation   : QRCoder
Image Storage   : Cloudinary
Email           : SMTP (Gmail)
Payment         : VNPay (VNPay.Secure)
```

### Frontend (Separate Repository)
```
Framework       : React 18 + TypeScript
Build Tool      : Vite
State Management: React Query / Context
Styling         : Tailwind CSS
Routing         : React Router
Deployment      : Cloudflare Pages
```

### Infrastructure
```
Cloud           : Microsoft Azure
CI/CD           : GitLab CI/CD
Domains         : Cloudflare Pages (Frontend)
API Host        : Azure App Service
Database        : Azure SQL Database
```

---

## 4. Core Features

### 4.1 Authentication & Authorization

```
┌─────────────────────────────────────────────────────────────────┐
│                    AUTHENTICATION FLOW                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   ┌──────────┐     ┌──────────┐     ┌──────────────────────┐   │
│   │ Register │────▶│  Send    │────▶│   Email Verification  │   │
│   │  (OTP)   │     │  Email   │     │   (30 min lifetime)   │   │
│   └──────────┘     └──────────┘     └──────────┬───────────┘   │
│                                                │               │
│   ┌──────────┐     ┌──────────┐     ┌──────────▼───────────┐   │
│   │  Login   │────▶│ Validate │────▶│  JWT + Refresh Token  │   │
│   │          │     │ Creds   │     │  (Rotation enabled)   │   │
│   └──────────┘     └──────────┘     └──────────────────────┘   │
│                                                                 │
│   Security Features:                                             │
│   ✓ Account Lockout (5 failed attempts → 15 min lock)           │
│   ✓ Rate Limiting (10 req/min general, 5 req/min strict)        │
│   ✓ XSS Protection via JWT claims                               │
│   ✓ Token Reuse Detection                                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.2 Movie & Showtime Management

| Feature | Description |
|---------|-------------|
| **Movie CRUD** | Full management với poster, trailer, duration, ratings |
| **Categories** | Genre-based classification |
| **Showtime Scheduling** | Automated overlap detection |
| **Theater Layout** | Auto-generated seat maps (VIP, Standard, Couple) |
| **Dynamic Pricing** | Seat type multipliers |

### 4.3 Booking System

```
┌─────────────────────────────────────────────────────────────────┐
│                    BOOKING WORKFLOW                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   1. Select Showtime ──▶ 2. Choose Seats ──▶ 3. Add F&B         │
│          │                    │                  │              │
│          ▼                    ▼                  ▼              │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              4. Apply Promotion (Optional)              │   │
│   └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              5. Create Booking → PENDING               │   │
│   │              - Hold seats for 10 minutes               │   │
│   │              - Generate payment record                  │   │
│   └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│                              ▼                                  │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │              6. Redirect to VNPay                       │   │
│   │              - Secure payment URL                       │   │
│   │              - Client IP captured                       │   │
│   └─────────────────────────────────────────────────────────┘   │
│                              │                                  │
│              ┌───────────────┴───────────────┐                  │
│              ▼                               ▼                  │
│   ┌─────────────────────┐       ┌─────────────────────────┐     │
│   │    PAYMENT SUCCESS   │       │   PAYMENT FAILED/       │     │
│   │  → CONFIRMED seats   │       │   TIMEOUT → EXPIRED    │     │
│   │  → QR Tickets gen    │       │  → Seats released      │     │
│   │  → Email sent        │       └─────────────────────────┘     │
│   └─────────────────────┘                                        │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.4 F&B (Food & Beverage) Management

| Feature | Description |
|---------|-------------|
| **Product Catalog** | Categories: Popcorn, Drinks, Combos |
| **Inventory Tracking** | Stock management per item |
| **Cart System** | Add/remove items before checkout |
| **Combo Discounts** | Bundled offers với promotion codes |

### 4.5 Payment Integration

```
┌─────────────────────────────────────────────────────────────────┐
│                    VNPAY INTEGRATION                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Supported Methods:                                              │
│   ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│   │  Credit     │  │  ATM        │  │   QR Code ( VietQR )    │ │
│   │  Card       │  │  Debit      │  │                         │ │
│   └─────────────┘  └─────────────┘  └─────────────────────────┘ │
│                                                                 │
│   Security:                                                      │
│   ✓ HMAC-SHA512 Signature Verification                           │
│   ✓ IPN (Instant Payment Notification) Handler                  │
│   ✓ Amount Re-verification on Return                           │
│   ✓ Dynamic Client IP Detection                                 │
│                                                                 │
│   Endpoints:                                                     │
│   - Payment URL generation                                       │
│   - Return URL (frontend redirect)                              │
│   - IPN URL (server-to-server callback)                         │
│   - Refund processing                                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.6 QR Ticket System

```
┌─────────────────────────────────────────────────────────────────┐
│                    QR TICKET FLOW                                │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   After successful payment:                                      │
│                                                                 │
│   ┌────────────────┐     ┌────────────────┐     ┌──────────────┐ │
│   │ Generate QR    │────▶│ Embed Booking  │────▶│ Send Email   │ │
│   │ Code per Seat │     │ + Seat Info    │     │ with PDF     │ │
│   └────────────────┘     └────────────────┘     └──────────────┘ │
│                                                          │      │
│                                                          ▼      │
│   ┌────────────────────────────────────────────────────────────┐ │
│   │                    QR Content Structure                     │ │
│   │  {                                                           │ │
│   │    "bookingRef": "CIN-ABC123",                              │ │
│   │    "seatCode": "A-12",                                      │ │
│   │    "showtime": "2026-07-28 19:30",                          │ │
│   │    "movie": "Inception",                                     │ │
│   │    "verify": "SHA256-HASH"                                  │ │
│   │  }                                                           │ │
│   └────────────────────────────────────────────────────────────┘ │
│                                                                 │
│   Check-in:                                                      │
│   ✓ Scan QR at theater entrance                                  │
│   ✓ Validate booking status                                     │
│   ✓ Mark seat as CHECKED_IN                                     │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 4.7 Promotion System

| Type | Description |
|------|-------------|
| **Percentage** | Giảm % tổng bill (VD: 10% off) |
| **Fixed Amount** | Giảm số tiền cố định (VD: 20,000đ) |
| **Buy X Get Y** | Mua X tặng Y (VD: Mua 2 vé tặng 1 nước) |
| **Min. Spend** | Áp dụng khi đạt ngưỡng tối thiểu |

---

## 5. API Summary

### 5.1 API Groups

| Group | Endpoints | Authentication | Description |
|-------|----------|----------------|-------------|
| **Auth** | 9 | No | Login, Register, Verify, Forgot Password |
| **Bookings** | 5 | Yes | Create, Get, Cancel, My Bookings |
| **Showtimes** | 3 | Partial | List, Seat Map |
| **Movies** | 8 | Admin | CRUD, Categories |
| **Theaters** | 5 | Admin | CRUD, Seats |
| **Payments** | 4 | No | VNPay callbacks, Status |
| **F&B** | 7 | Yes | Products, Cart, Orders |
| **Promotions** | 5 | Yes | Codes, Apply |
| **Users** | 4 | Yes | Profile, Password |
| **Staff POS** | 6 | Staff | Walk-in booking, F&B orders |

### 5.2 Authentication Flow

```
┌─────────────────────────────────────────────────────────────────┐
│              JWT + REFRESH TOKEN FLOW                            │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│   Login Response:                                                │
│   ┌─────────────────────────────────────────────────────────┐   │
│   │ {                                                         │   │
│   │   "accessToken": "eyJhbG...",      // 2 hours validity   │   │
│   │   "refreshToken": "dGhpcyI...",      // 30 days validity  │   │
│   │   "expiresAt": "2026-07-28T21:00:00Z"                    │   │
│   │ }                                                         │   │
│   └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│   Refresh Token Flow:                                            │
│   1. Access token expires                                      │
│   2. Client calls POST /auth/refresh-token                     │
│   3. Server validates refresh token                            │
│   4. Issues new access + refresh token pair                   │
│   5. Old refresh token is REVOKED (rotation)                   │
│                                                                 │
│   Token Revocation (Logout):                                    │
│   - All user tokens invalidated immediately                    │
│   - Token reuse detection triggers full revocation             │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### 5.3 Response Format

```json
// Success Response
{
  "isSuccess": true,
  "message": "Operation completed successfully.",
  "data": { ... }
}

// Error Response
{
  "isSuccess": false,
  "message": "Error description.",
  "errors": ["Detailed error 1", "Detailed error 2"]
}
```

---

## 6. Security Features

### 6.1 Security Matrix

| Feature | Implementation | Status |
|---------|---------------|--------|
| **Authentication** | JWT Bearer + Refresh Token Rotation | ✅ |
| **Password Hashing** | BCrypt (cost factor 12) | ✅ |
| **Account Lockout** | 5 failed attempts → 15 min lock | ✅ |
| **Rate Limiting** | IP-based, configurable per endpoint | ✅ |
| **CORS** | Configurable allowed origins | ✅ |
| **Input Validation** | FluentValidation on all DTOs | ✅ |
| **SQL Injection** | Entity Framework parameterized queries | ✅ |
| **XSS** | JSON-only API, no raw HTML | ✅ |
| **CSRF** | Stateless JWT, not cookie-based | ✅ |
| **IDOR Protection** | Ownership validation in services | ✅ |
| **Email Enumeration** | Generic error messages | ✅ |
| **Sensitive Data** | Config in user-secrets, not in code | ✅ |

### 6.2 Rate Limiting Policies

| Policy | Limit | Applied To |
|--------|-------|------------|
| `auth` | 10 requests/minute | All auth endpoints |
| `auth-strict` | 5 requests/minute | Login, Forgot Password |

---

## 7. Database Schema (Core Entities)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           CORE ENTITIES                                     │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  ┌──────────┐     ┌──────────┐     ┌──────────┐     ┌──────────────────┐  │
│  │   User   │     │  Movie   │     │ Theater │     │   Showtime       │  │
│  ├──────────┤     ├──────────┤     ├──────────┤     ├──────────────────┤  │
│  │ Id       │     │ Id       │     │ Id      │     │ Id               │  │
│  │ Email    │     │ Title    │     │ Name    │     │ MovieId (FK)     │  │
│  │ Password │     │ Poster   │     │ Rows    │     │ TheaterId (FK)   │  │
│  │ Role     │◀────│ Duration │     │ Seats   │◀────│ StartTime        │  │
│  │ IsEmail  │     │ Rating   │     └──────────┘     │ EndTime          │  │
│  │ Verified │     └──────────┘                       │ BasePrice        │  │
│  └──────────┘                                          └──────────────────┘  │
│       │                                                       │             │
│       │                                    ┌───────────────────┘             │
│       │                                    │                                  │
│       ▼                                    ▼                                  │
│  ┌──────────┐     ┌──────────┐     ┌──────────────────┐     ┌───────────┐ │
│  │ Booking │◀───▶│  Seat    │◀───▶│ ShowtimeSeat     │     │ SeatType  │ │
│  ├──────────┤     ├──────────┤     ├──────────────────┤     ├───────────┤ │
│  │ Id      │     │ Id       │     │ Id               │     │ Id        │ │
│  │ UserId  │     │ TheaterId│     │ ShowtimeId (FK) │     │ Name      │ │
│  │ Showtime│     │ Row      │     │ SeatId (FK)      │     │ Multiplier│ │
│  │ Status  │     │ Number   │     │ Status           │     └───────────┘ │
│  │ Total   │     │ TypeId   │     └──────────────────┘                    │
│  │ Payment │────▶│ Status   │                                            │
│  └──────────┘     └──────────┘                                            │
│       │                                                                  │
│       │     ┌────────────┐     ┌──────────────┐                          │
│       │     │  Payment   │     │ Promotion    │                          │
│       └───▶ │            │     │              │                          │
│             ├────────────┤     ├──────────────┤                          │
│             │ Id         │     │ Id           │                          │
│             │ BookingId  │     │ Code         │                          │
│             │ Amount     │     │ Type         │                          │
│             │ Gateway    │     │ Discount     │                          │
│             │ Status     │     │ MinAmount    │                          │
│             │ TxnRef     │     │ ValidUntil   │                          │
│             └────────────┘     └──────────────┘                          │
│                                                                             │
│  ┌──────────────┐     ┌────────────┐     ┌────────────────────────────┐  │
│  │ FnbOrder     │     │ FnbProduct │     │ RefreshToken               │  │
│  ├──────────────┤     ├────────────┤     ├────────────────────────────┤  │
│  │ Id           │     │ Id         │     │ Id                        │  │
│  │ BookingId(FK)│     │ Name       │     │ UserId (FK)               │  │
│  │ TotalAmount  │     │ Category   │     │ Token                     │  │
│  │ Status       │     │ Price      │     │ ExpiresAt                 │  │
│  └──────────────┘     │ Stock      │     │ IsRevoked                 │  │
│         │              └────────────┘     │ DeviceInfo                │  │
│         │                                 └────────────────────────────┘  │
│         ▼                                                                  │
│  ┌─────────────────────┐                                                   │
│  │ FnbOrderDetail      │                                                   │
│  ├─────────────────────┤                                                   │
│  │ Id                   │                                                   │
│  │ FnbOrderId (FK)     │                                                   │
│  │ FnbProductId (FK)   │                                                   │
│  │ Quantity            │                                                   │
│  │ UnitPrice           │                                                   │
│  └─────────────────────┘                                                   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 8. Business Rules Summary

### 8.1 Booking Rules

| Rule | Description |
|------|-------------|
| **Seat Hold** | Seats held for 10 minutes after booking |
| **Seat Expiry** | Booking expires if not paid within hold period |
| **Cancellation** | Only PENDING/CONFIRMED bookings can be cancelled |
| **Refund** | Full refund for cancelled bookings |
| **Seat Multiplier** | VIP seats: 1.5x, Couple: 2x, Standard: 1x |

### 8.2 Showtime Rules

| Rule | Description |
|------|-------------|
| **Overlap Check** | Cannot schedule overlapping showtimes in same theater |
| **Cleanup** | Showtime auto-completes when current time > end time |
| **Buffer Time** | 15-minute buffer between consecutive showtimes |

### 8.3 User Rules

| Rule | Description |
|------|-------------|
| **Email Verification** | Required before first login (30-min token) |
| **Account Lockout** | 5 failed logins → 15-minute lockout |
| **Roles** | Admin, Staff, Customer, Guest |

---

## 9. Deployment Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         DEPLOYMENT ARCHITECTURE                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│                         ┌─────────────────────────┐                        │
│                         │      Internet           │                        │
│                         └───────────┬─────────────┘                        │
│                                     │                                       │
│         ┌───────────────────────────┼───────────────────────────┐            │
│         │                           │                           │            │
│         ▼                           ▼                           │            │
│  ┌─────────────────┐      ┌─────────────────┐                  │            │
│  │ Cloudflare      │      │ GitLab          │                  │            │
│  │ Pages            │      │ Repository      │                  │            │
│  │ (Frontend)       │      │                  │                  │            │
│  │ dc31b3c6.cinema- │      │ CI/CD Pipeline   │                  │            │
│  │ system-fe.pages. │      │                  │                  │            │
│  │ dev              │      │                  │                  │            │
│  └────────┬─────────┘      └────────┬─────────┘                  │            │
│           │ HTTPS                     │                             │            │
│           ▼                           ▼                             │            │
│  ┌────────────────────────────────────────────────────────────────┐ │       │
│  │                    Azure App Service                           │ │       │
│  │                    (CinemaSystem.API)                           │ │       │
│  │                                                                 │ │       │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐│ │       │
│  │  │ Auto-scale   │  │ Health Check │  │  Managed Identity   ││ │       │
│  │  │ (1-3 inst.)  │  │ /health       │  │  (Key Vault access)  ││ │       │
│  │  └──────────────┘  └──────────────┘  └──────────────────────┘│ │       │
│  └────────────────────────────────────────────────────────────────┘ │       │
│                                     │                             │       │
│                                     │ SQL Connection             │       │
│                                     │ Managed Firewall           │       │
│                                     ▼                             │       │
│                         ┌─────────────────────────┐                │       │
│                         │   Azure SQL Database   │◀───────────────┘       │
│                         │   (cinema_db)           │                        │
│                         │   - Geo-replication    │                        │
│                         │   - Auto-backup        │                        │
│                         │   - TDE encryption     │                        │
│                         └─────────────────────────┘                        │
│                                                                             │
│                         ┌─────────────────────────┐                        │
│                         │   External Services     │                        │
│                         ├─────────────────────────┤                        │
│                         │ • Cloudinary (Images)   │                        │
│                         │ • VNPay (Payments)      │                        │
│                         │ • Gmail SMTP (Email)    │                        │
│                         │ • Google OAuth          │                        │
│                         └─────────────────────────┘                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Environment Configuration

| Environment | Frontend URL | API URL | Purpose |
|-------------|--------------|---------|---------|
| **Development** | localhost:5173 | localhost:7121 | Local development |
| **Staging** | TBD | TBD | Pre-production testing |
| **Production** | dc31b3c6.cinema-system-fe.pages.dev | dc31b3c6.cinema-system-be.pages.dev | Live system |

---

## 10. Background Services

| Service | Schedule | Function |
|---------|----------|----------|
| **BookingExpiryService** | Every 1 min | Release expired PENDING bookings, free seats |
| **ShowtimeCompletionService** | Every 5 min | Mark past showtimes as COMPLETED |
| **RefreshTokenCleanupService** | Daily | Remove expired/revoked tokens |

---

## 11. Monitoring & Health

### Health Check Endpoint
```
GET /health
Response: { "status": "Healthy", "timestamp": "..." }
```

### Key Metrics to Monitor
- API Response Time (P95 < 500ms)
- Error Rate (< 1%)
- Payment Success Rate (> 95%)
- Booking Conversion Rate
- Active User Sessions
- Database Connection Pool

---

## 12. Future Roadmap

### Short Term (Q3 2026)
- [ ] Email notifications for booking reminders
- [ ] Audit logging for admin actions
- [ ] Dashboard analytics for admins
- [ ] PDF ticket download

### Medium Term (Q4 2026)
- [ ] Mobile app (React Native)
- [ ] Loyalty points system
- [ ] Seat selection preview with 3D theater view
- [ ] Real-time seat availability updates (SignalR)

### Long Term (2027)
- [ ] Multi-cinema support
- [ ] Movie rental/streaming integration
- [ ] AI-based recommendation engine
- [ ] Kubernetes deployment

---

## 13. Contact & Support

| Role | Contact |
|------|---------|
| **System Administrator** | admin@cinema-system.com |
| **Technical Lead** | tech-lead@cinema-system.com |
| **API Support** | api-support@cinema-system.com |

### Documentation
- API Documentation: `/swagger`
- Architecture Docs: `/docs`
- Deployment Guide: [Internal Wiki]

---

*This document is for internal use only. Do not distribute externally without approval.*
