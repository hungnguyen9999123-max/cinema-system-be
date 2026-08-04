using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class CinemaDbContext : DbContext
{
    public CinemaDbContext()
    {
    }

    public CinemaDbContext(DbContextOptions<CinemaDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AudienceType> AudienceTypes { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ChatConversation> ChatConversations { get; set; }

    public virtual DbSet<ChatMessage> ChatMessages { get; set; }

    public virtual DbSet<ChatParticipant> ChatParticipants { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<BookingSeat> BookingSeats { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<EmailVerificationToken> EmailVerificationTokens { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<FnbItem> FnbItems { get; set; }

    public virtual DbSet<FnbOrder> FnbOrders { get; set; }

    public virtual DbSet<FnbOrderDetail> FnbOrderDetails { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<PricingRule> PricingRules { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<PromotionUsage> PromotionUsages { get; set; }

    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    public virtual DbSet<Refund> Refunds { get; set; }

    public virtual DbSet<RefundGatewayAttempt> RefundGatewayAttempts { get; set; }

    public virtual DbSet<Wallet> Wallets { get; set; }

    public virtual DbSet<WalletTransaction> WalletTransactions { get; set; }

    public virtual DbSet<WalletTopUp> WalletTopUps { get; set; }

    public virtual DbSet<WithdrawalRequest> WithdrawalRequests { get; set; }

    public virtual DbSet<Room> Rooms { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<SeatType> SeatTypes { get; set; }

    public virtual DbSet<Showtime> Showtimes { get; set; }

    public virtual DbSet<StaffAssignment> StaffAssignments { get; set; }

    public virtual DbSet<Ticket> Tickets { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlServer(GetConnectionString());
        }
    }
    private string GetConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", true, true)
            .Build();

        var strConn = config["ConnectionStrings:DefaultConnection"];

        // Nếu strConn là null, trả về chuỗi kết nối bypass bên dưới
        return strConn ?? "Server=localhost;Database=cinema_db;Trusted_Connection=True;TrustServerCertificate=True;";
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseCollation("Vietnamese_CI_AS");

        modelBuilder.Entity<AudienceType>(entity =>
        {
            entity.ToTable("AUDIENCE_TYPES");

            entity.HasIndex(e => e.Code, "UQ_AUDIENCE_TYPES_CODE").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AudienceMultiplier)
                .HasDefaultValue(1.00m)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("audience_multiplier");
            entity.Property(e => e.Code)
                .HasMaxLength(20)
                .HasColumnName("code");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(50)
                .HasColumnName("display_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AUDIT_LOGS");

            entity.HasIndex(e => e.ActorId, "IX_AL_ACTOR");

            entity.HasIndex(e => e.CreatedAt, "IX_AL_CREATED");

            entity.HasIndex(e => new { e.EntityName, e.EntityId }, "IX_AL_ENTITY");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(20)
                .HasColumnName("action_type");
            entity.Property(e => e.ActorId).HasColumnName("actor_id");
            entity.Property(e => e.AfterData).HasColumnName("after_data");
            entity.Property(e => e.BeforeData).HasColumnName("before_data");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Endpoint)
                .HasMaxLength(255)
                .HasColumnName("endpoint");
            entity.Property(e => e.EntityId).HasColumnName("entity_id");
            entity.Property(e => e.EntityName)
                .HasMaxLength(100)
                .HasColumnName("entity_name");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasColumnName("ip_address");

            entity.HasOne(d => d.Actor).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.ActorId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_AL_ACTOR");
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.ToTable("BOOKINGS");

            entity.HasIndex(e => e.CustomerId, "IX_BK_CUSTOMER");

            entity.HasIndex(e => new { e.CustomerId, e.Status }, "IX_BK_CUST_STATUS");

            entity.HasIndex(e => new { e.ExpiresAt, e.Status }, "IX_BK_EXPIRES_STATUS").HasFilter("([status]='PENDING')");

            entity.HasIndex(e => e.ShowtimeId, "IX_BK_SHOWTIME");

            entity.HasIndex(e => e.Status, "IX_BK_STATUS");

            entity.HasIndex(e => new { e.Id, e.ShowtimeId }, "UQ_BOOKINGS_ID_SHOWTIME").IsUnique();

            entity.HasIndex(e => e.BookingRef, "UQ_BOOKINGS_REF").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BookedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("booked_at");
            entity.Property(e => e.BookingRef)
                .HasMaxLength(20)
                .HasColumnName("booking_ref");
            entity.Property(e => e.CancelledAt).HasColumnName("cancelled_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.DiscountAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("discount_amount");
            entity.Property(e => e.ExpiresAt)
                .HasDefaultValueSql("(dateadd(minute,(10),sysdatetime()))")
                .HasColumnName("expires_at");
            entity.Property(e => e.FinalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("final_amount");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_amount");

            entity.HasOne(d => d.Customer).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BK_CUSTOMER");

            entity.HasOne(d => d.Promotion).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_BK_PROMOTION");

            entity.HasOne(d => d.Showtime).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BK_SHOWTIME");
        });

        modelBuilder.Entity<BookingSeat>(entity =>
        {
            entity.ToTable("BOOKING_SEATS");

            entity.HasIndex(e => e.BookingId, "IX_BS_BOOKING");

            entity.HasIndex(e => e.SeatId, "IX_BS_SEAT");

            entity.HasIndex(e => e.ShowtimeId, "IX_BS_SHOWTIME");

            entity.HasIndex(e => e.SeatStatus, "IX_BS_STATUS");

            entity.HasIndex(e => new { e.Id, e.BookingId }, "UQ_BS_ID_BOOKING").IsUnique();

            entity.HasIndex(e => new { e.SeatId, e.ShowtimeId }, "UQ_BS_SEAT_SHOWTIME")
                .IsUnique()
                .HasFilter("([seat_status] IN ('HELD','BOOKED','CONFIRMED'))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AudienceMultSnap)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("audience_mult_snap");
            entity.Property(e => e.AudienceTypeId).HasColumnName("audience_type_id");
            entity.Property(e => e.BasePriceSnap)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("base_price_snap");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.PricingRuleId).HasColumnName("pricing_rule_id");
            entity.Property(e => e.SeatId).HasColumnName("seat_id");
            entity.Property(e => e.SeatMultSnap)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("seat_mult_snap");
            entity.Property(e => e.SeatStatus)
                .HasMaxLength(20)
                .HasDefaultValue("HELD")
                .HasColumnName("seat_status");
            entity.Property(e => e.ShowtimeId).HasColumnName("showtime_id");
            entity.Property(e => e.TimeMultSnap)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("time_mult_snap");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.AudienceType).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.AudienceTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_AUDIENCE_TYPE");

            entity.HasOne(d => d.Booking).WithMany(p => p.BookingSeatBookings)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_BS_BOOKING");

            entity.HasOne(d => d.PricingRule).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.PricingRuleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_PRICING_RULE");

            entity.HasOne(d => d.Seat).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_SEAT");

            entity.HasOne(d => d.Showtime).WithMany(p => p.BookingSeats)
                .HasForeignKey(d => d.ShowtimeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_SHOWTIME");

            entity.HasOne(d => d.BookingNavigation).WithMany(p => p.BookingSeatBookingNavigations)
                .HasPrincipalKey(p => new { p.Id, p.ShowtimeId })
                .HasForeignKey(d => new { d.BookingId, d.ShowtimeId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_BS_BOOKING_SHOWTIME");
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("CINEMAS");

            entity.HasIndex(e => e.City, "IX_CINEMAS_CITY");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Address).HasColumnName("address");
            entity.Property(e => e.City)
                .HasMaxLength(100)
                .HasColumnName("city");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.ToTable("EMAIL_VERIFICATION_TOKENS");

            entity.HasIndex(e => e.ExpiresAt, "IX_EMAIL_VERIFICATION_TOKENS_EXPIRES");

            entity.HasIndex(e => e.UserId, "IX_EMAIL_VERIFICATION_TOKENS_USER");

            entity.HasIndex(e => e.TokenHash, "UQ_EMAIL_VERIFICATION_TOKENS_HASH").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_EMAIL_VERIFICATION_TOKENS_USER_ACTIVE")
                .IsUnique()
                .HasFilter("([is_verified]=(0))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsVerified).HasColumnName("is_verified");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerifiedAt).HasColumnName("verified_at");
            entity.Property(e => e.VerifiedByIp)
                .HasMaxLength(45)
                .HasColumnName("verified_by_ip");

            entity.HasOne(d => d.User).WithOne(p => p.EmailVerificationToken)
                .HasForeignKey<EmailVerificationToken>(d => d.UserId)
                .HasConstraintName("FK_EMAIL_VERIFICATION_TOKENS_USER");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.ToTable("FEEDBACKS");

            entity.HasIndex(e => e.BookingId, "IX_FB_BOOKING");

            entity.HasIndex(e => e.MovieId, "IX_FB_MOVIE");

            entity.HasIndex(e => new { e.CustomerId, e.BookingId }, "UQ_FB_CUSTOMER_BOOKING").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.Comment).HasColumnName("comment");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.IsApproved).HasColumnName("is_approved");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.Rating).HasColumnName("rating");

            entity.HasOne(d => d.Booking).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FB_BOOKING");

            entity.HasOne(d => d.Customer).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FB_CUSTOMER");

            entity.HasOne(d => d.Movie).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FB_MOVIE");
        });

        modelBuilder.Entity<FnbItem>(entity =>
        {
            entity.ToTable("FNB_ITEMS");

            entity.HasIndex(e => e.Status, "IX_FNB_ITEMS_STATUS");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasColumnName("category");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.ImagePublicId)
                .HasMaxLength(255)
                .HasColumnName("image_public_id");
            entity.Property(e => e.ImageUrl)
                .HasMaxLength(500)
                .HasColumnName("image_url");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("price");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.FnbItems)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FNB_ITEMS_CREATOR");
        });

        modelBuilder.Entity<FnbOrder>(entity =>
        {
            entity.ToTable("FNB_ORDERS");

            entity.HasIndex(e => e.BookingId, "IX_FNB_ORDERS_BOOKING");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.OrderStatus)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("order_status");
            entity.Property(e => e.TotalAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("total_amount");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");
            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .HasColumnName("payment_method");

            entity.HasOne(d => d.Booking).WithMany(p => p.FnbOrders)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FNB_ORDERS_BOOKING");

            entity.HasOne(d => d.Customer).WithMany(p => p.FnbOrders)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FNB_ORDERS_CUSTOMER");

            entity.HasOne(d => d.Staff).WithMany()
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_FNB_ORDERS_STAFF");
        });

        modelBuilder.Entity<FnbOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_FNB_OD");

            entity.ToTable("FNB_ORDER_DETAILS");

            entity.HasIndex(e => e.FnbOrderId, "IX_FNB_OD_ORDER");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.FnbOrderId).HasColumnName("fnb_order_id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.Subtotal)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("subtotal");
            entity.Property(e => e.UnitPrice)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("unit_price");

            entity.HasOne(d => d.FnbOrder).WithMany(p => p.FnbOrderDetails)
                .HasForeignKey(d => d.FnbOrderId)
                .HasConstraintName("FK_FNB_OD_ORDER");

            entity.HasOne(d => d.Item).WithMany(p => p.FnbOrderDetails)
                .HasForeignKey(d => d.ItemId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_FNB_OD_ITEM");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.ToTable("MOVIES");

            entity.HasIndex(e => e.Status, "IX_MOVIES_STATUS");

            entity.HasIndex(e => e.Title, "IX_MOVIES_TITLE");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AgeRating)
                .HasMaxLength(10)
                .HasColumnName("age_rating");
            entity.Property(e => e.BannerPublicId)
                .HasMaxLength(255)
                .HasColumnName("banner_public_id");
            entity.Property(e => e.BannerUrl)
                .HasMaxLength(500)
                .HasColumnName("banner_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DurationMin).HasColumnName("duration_min");
            entity.Property(e => e.Genre)
                .HasMaxLength(100)
                .HasColumnName("genre");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .HasColumnName("language");
            entity.Property(e => e.PosterPublicId)
                .HasMaxLength(255)
                .HasColumnName("poster_public_id");
            entity.Property(e => e.PosterUrl)
                .HasMaxLength(500)
                .HasColumnName("poster_url");
            entity.Property(e => e.ReleaseDate).HasColumnName("release_date");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("UPCOMING")
                .HasColumnName("status");
            entity.Property(e => e.Synopsis).HasColumnName("synopsis");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.TrailerUrl)
                .HasMaxLength(500)
                .HasColumnName("trailer_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Movies)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_MOVIES_CREATOR");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("NOTIFICATIONS");

            entity.HasIndex(e => new { e.Status, e.RetryCount }, "IX_NOTIF_STATUS");

            entity.HasIndex(e => e.UserId, "IX_NOTIF_USER");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Body).HasColumnName("body");
            entity.Property(e => e.Channel)
                .HasMaxLength(20)
                .HasColumnName("channel");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.RetryCount).HasColumnName("retry_count");
            entity.Property(e => e.SentAt).HasColumnName("sent_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");
            entity.Property(e => e.Subject)
                .HasMaxLength(255)
                .HasColumnName("subject");
            entity.Property(e => e.Type)
                .HasMaxLength(30)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_NOTIF_USER");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PASSWORD_RESET_TOKENS");

            entity.HasIndex(e => e.ExpiresAt, "IX_PASSWORD_RESET_TOKENS_EXPIRES");

            entity.HasIndex(e => e.UserId, "IX_PASSWORD_RESET_TOKENS_USER");

            entity.HasIndex(e => e.TokenHash, "UQ_PASSWORD_RESET_TOKENS_HASH").IsUnique();

            entity.HasIndex(e => e.UserId, "UQ_PASSWORD_RESET_TOKENS_USER_ACTIVE")
                .IsUnique()
                .HasFilter("([is_used]=(0))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsUsed).HasColumnName("is_used");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UsedAt).HasColumnName("used_at");
            entity.Property(e => e.UsedByIp)
                .HasMaxLength(45)
                .HasColumnName("used_by_ip");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.PasswordResetToken)
                .HasForeignKey<PasswordResetToken>(d => d.UserId)
                .HasConstraintName("FK_PASSWORD_RESET_TOKENS_USER");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("PAYMENTS");

            entity.HasIndex(e => e.BookingId, "IX_PAY_BOOKING");

            entity.HasIndex(e => e.Gateway, "IX_PAY_GATEWAY");

            entity.HasIndex(e => e.Status, "IX_PAY_STATUS");

            entity.HasIndex(e => new { e.BookingId, e.IdempotencyKeyHash }, "UQ_PAY_BOOKING_IDEMPOTENCY")
                .IsUnique()
                .HasFilter("[idempotency_key_hash] IS NOT NULL");

            entity.HasIndex(e => e.BookingId, "UQ_PAY_BOOKING_SUCCESS")
                .IsUnique()
                .HasFilter("[status] = 'SUCCESS'");

            entity.HasIndex(e => e.GatewayTxnId, "UX_PAYMENTS_GATEWAY_TXN")
                .IsUnique()
                .HasFilter("([gateway_txn_id] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.FnbOrderId).HasColumnName("fnb_order_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Gateway)
                .HasMaxLength(20)
                .HasColumnName("gateway");
            entity.Property(e => e.GatewayTxnId)
                .HasMaxLength(100)
                .HasColumnName("gateway_txn_id");
            entity.Property(e => e.GatewayRequestAt).HasColumnName("gateway_request_at");
            entity.Property(e => e.IpnSignature)
                .HasMaxLength(255)
                .HasColumnName("ipn_signature");
            entity.Property(e => e.IdempotencyKeyHash)
                .HasMaxLength(128)
                .HasColumnName("idempotency_key_hash");
            entity.Property(e => e.PaidAt).HasColumnName("paid_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");

            entity.HasOne(d => d.Booking).WithMany(p => p.Payments)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PAY_BOOKING");

            entity.HasOne(d => d.FnbOrder).WithMany(p => p.Payments)
                .HasForeignKey(d => d.FnbOrderId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_PAY_FNB_ORDER");
        });

        modelBuilder.Entity<PricingRule>(entity =>
        {
            entity.ToTable("PRICING_RULES");

            entity.HasIndex(e => new { e.CinemaId, e.RoomTypeId, e.TimeSlotId, e.IsActive, e.EffectiveFrom, e.EffectiveTo }, "IX_PR_LOOKUP");

            entity.HasIndex(e => new { e.CinemaId, e.RoomTypeId, e.TimeSlotId, e.EffectiveFrom, e.EffectiveTo }, "UX_PR_ACTIVE_COMBO_DATES")
                .IsUnique()
                .HasFilter("([is_active]=(1))");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BasePrice)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("base_price");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.RoomTypeId).HasColumnName("room_type_id");
            entity.Property(e => e.TimeMultiplier)
                .HasDefaultValue(1.00m)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("time_multiplier");
            entity.Property(e => e.TimeSlotId).HasColumnName("time_slot_id");

            entity.HasOne(d => d.Cinema).WithMany(p => p.PricingRules)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PR_CINEMA");
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.ToTable("PROMOTIONS");

            entity.HasIndex(e => new { e.IsActive, e.ValidFrom, e.ValidTo }, "IX_PROMO_ACTIVE");

            entity.HasIndex(e => e.PromoCode, "IX_PROMO_CODE");

            entity.HasIndex(e => e.PromoCode, "UQ_PROMOTIONS_CODE").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.DiscountType)
                .HasMaxLength(20)
                .HasColumnName("discount_type");
            entity.Property(e => e.DiscountValue)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("discount_value");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MinOrderAmt)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("min_order_amt");
            entity.Property(e => e.Name)
                .HasMaxLength(150)
                .HasColumnName("name");
            entity.Property(e => e.PromoCode)
                .HasMaxLength(20)
                .HasColumnName("promo_code");
            entity.Property(e => e.UsageLimit).HasColumnName("usage_limit");
            entity.Property(e => e.ValidFrom).HasColumnName("valid_from");
            entity.Property(e => e.ValidTo).HasColumnName("valid_to");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Promotions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PROMO_CREATOR");
        });

        modelBuilder.Entity<PromotionUsage>(entity =>
        {
            entity.ToTable("PROMOTION_USAGES");

            entity.HasIndex(e => e.CustomerId, "IX_PU_CUSTOMER");

            entity.HasIndex(e => e.PromotionId, "IX_PU_PROMOTION");

            entity.HasIndex(e => new { e.PromotionId, e.CustomerId }, "UQ_PU_PROMO_CUSTOMER").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.CustomerId).HasColumnName("customer_id");
            entity.Property(e => e.PromotionId).HasColumnName("promotion_id");
            entity.Property(e => e.UsedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("used_at");

            entity.HasOne(d => d.Booking).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PU_BOOKING");

            entity.HasOne(d => d.Customer).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PU_CUSTOMER");

            entity.HasOne(d => d.Promotion).WithMany(p => p.PromotionUsages)
                .HasForeignKey(d => d.PromotionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PU_PROMOTION");
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("REFRESH_TOKENS");

            entity.HasIndex(e => e.ExpiresAt, "IX_REFRESH_TOKENS_EXPIRES");

            entity.HasIndex(e => e.UserId, "IX_REFRESH_TOKENS_USER");

            entity.HasIndex(e => e.UserId, "UQ_REFRESH_TOKENS_ACTIVE_USER")
                .IsUnique()
                .HasFilter("([is_revoked]=(0))");

            entity.HasIndex(e => e.TokenHash, "UQ_REFRESH_TOKENS_HASH").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newid())")
                .HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysutcdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedByIp)
                .HasMaxLength(45)
                .HasColumnName("created_by_ip");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.IsRevoked).HasColumnName("is_revoked");
            entity.Property(e => e.ReplacedByToken)
                .HasMaxLength(255)
                .HasColumnName("replaced_by_token");
            entity.Property(e => e.RevokedAt).HasColumnName("revoked_at");
            entity.Property(e => e.RevokedByIp)
                .HasMaxLength(45)
                .HasColumnName("revoked_by_ip");
            entity.Property(e => e.TokenHash)
                .HasMaxLength(255)
                .HasColumnName("token_hash");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithOne(p => p.RefreshToken)
                .HasForeignKey<RefreshToken>(d => d.UserId)
                .HasConstraintName("FK_REFRESH_TOKENS_USERS");
        });

        modelBuilder.Entity<Refund>(entity =>
        {
            entity.ToTable("REFUNDS");

            entity.HasIndex(e => e.PaymentId, "UQ_REFUNDS_PAYMENT_ACTIVE")
                .IsUnique()
                .HasFilter("[status] IN ('REQUESTED','PROCESSING','RECONCILIATION_REQUIRED')");

            entity.HasIndex(e => e.PaymentId, "IX_REF_PAYMENT");

            entity.HasIndex(e => e.Status, "IX_REF_STATUS");

            entity.HasIndex(e => new { e.RequestedBy, e.IdempotencyKeyHash }, "UX_REF_REQUESTER_IDEMPOTENCY")
                .IsUnique()
                .HasFilter("([requested_by] IS NOT NULL AND [idempotency_key_hash] IS NOT NULL)");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.GatewayRefundId)
                .HasMaxLength(100)
                .HasColumnName("gateway_refund_id");
            entity.Property(e => e.IdempotencyKeyHash)
                .HasMaxLength(128)
                .HasColumnName("idempotency_key_hash");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ReasonCode)
                .HasMaxLength(50)
                .HasColumnName("reason_code");
            entity.Property(e => e.DecisionReason)
                .HasMaxLength(500)
                .HasColumnName("decision_reason");
            entity.Property(e => e.FailureCode)
                .HasMaxLength(50)
                .HasColumnName("failure_code");
            entity.Property(e => e.FailureMessage)
                .HasMaxLength(500)
                .HasColumnName("failure_message");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.DecidedAt).HasColumnName("decided_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.NextReconciliationAt).HasColumnName("next_reconciliation_at");
            entity.Property(e => e.ReprocessCount).HasColumnName("reprocess_count");
            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .IsConcurrencyToken()
                .HasColumnName("row_version");
            entity.Property(e => e.Reason)
                .HasMaxLength(500)
                .HasColumnName("reason");
            entity.Property(e => e.RefundAmount)
                .HasColumnType("decimal(12, 2)")
                .HasColumnName("refund_amount");
            entity.Property(e => e.RequestedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("requested_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("PENDING")
                .HasColumnName("status");

            entity.HasOne(d => d.Payment).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_REF_PAYMENT");

            entity.HasOne(d => d.ProcessedByNavigation).WithMany(p => p.Refunds)
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_REF_PROCESSOR");

            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.RequestedRefunds)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_REF_REQUESTER");
        });

        modelBuilder.Entity<RefundGatewayAttempt>(entity =>
        {
            entity.ToTable("REFUND_GATEWAY_ATTEMPTS");

            entity.HasIndex(e => new { e.RefundId, e.AttemptNo }, "UQ_REF_ATTEMPT_NO").IsUnique();
            entity.HasIndex(e => e.MerchantRequestId, "UQ_REF_ATTEMPT_REQUEST_ID").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            entity.Property(e => e.AttemptNo).HasColumnName("attempt_no");
            entity.Property(e => e.Operation).HasMaxLength(20).HasColumnName("operation");
            entity.Property(e => e.MerchantRequestId).HasMaxLength(32).HasColumnName("merchant_request_id");
            entity.Property(e => e.Status).HasMaxLength(30).HasColumnName("status");
            entity.Property(e => e.RequestDigest).HasMaxLength(128).HasColumnName("request_digest");
            entity.Property(e => e.SubmittedAt).HasColumnName("submitted_at");
            entity.Property(e => e.RespondedAt).HasColumnName("responded_at");
            entity.Property(e => e.GatewayResponseId).HasMaxLength(32).HasColumnName("gateway_response_id");
            entity.Property(e => e.GatewayTransactionNo).HasMaxLength(100).HasColumnName("gateway_transaction_no");
            entity.Property(e => e.ResponseCode).HasMaxLength(10).HasColumnName("response_code");
            entity.Property(e => e.TransactionStatus).HasMaxLength(10).HasColumnName("transaction_status");
            entity.Property(e => e.ResponseMessage).HasMaxLength(500).HasColumnName("response_message");

            entity.HasOne(d => d.Refund).WithMany(p => p.GatewayAttempts)
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_REF_ATTEMPT_REFUND");
        });

        modelBuilder.Entity<Wallet>(entity =>
        {
            entity.ToTable("WALLETS");
            entity.HasIndex(e => e.UserId, "UQ_WALLETS_USER").IsUnique();
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())").HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Balance).HasColumnType("decimal(18, 2)").HasColumnName("balance");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken().HasColumnName("row_version");
            entity.HasOne(d => d.User).WithOne(p => p.Wallet)
                .HasForeignKey<Wallet>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WALLETS_USERS");
        });

        modelBuilder.Entity<WalletTransaction>(entity =>
        {
            entity.ToTable("WALLET_TRANSACTIONS");
            entity.HasIndex(e => new { e.WalletId, e.CreatedAt }, "IX_WALLET_TX_WALLET_CREATED");
            entity.HasIndex(e => e.RefundId, "UQ_WALLET_TX_REFUND")
                .IsUnique()
                .HasFilter("[refund_id] IS NOT NULL");
            entity.HasIndex(e => e.PaymentId, "UQ_WALLET_TX_PAYMENT")
                .IsUnique()
                .HasFilter("[payment_id] IS NOT NULL");
            entity.HasIndex(e => e.WalletTopUpId, "UQ_WALLET_TX_TOPUP")
                .IsUnique()
                .HasFilter("[wallet_topup_id] IS NOT NULL");
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())").HasColumnName("id");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.RefundId).HasColumnName("refund_id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.WalletTopUpId).HasColumnName("wallet_topup_id");
            entity.Property(e => e.WithdrawalRequestId).HasColumnName("withdrawal_request_id");
            entity.Property(e => e.Type).HasMaxLength(30).HasColumnName("type");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("amount");
            entity.Property(e => e.BalanceAfter).HasColumnType("decimal(18, 2)").HasColumnName("balance_after");
            entity.Property(e => e.Description).HasMaxLength(500).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.HasOne(d => d.Wallet).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WALLET_TX_WALLET");
            entity.HasOne(d => d.Refund).WithMany()
                .HasForeignKey(d => d.RefundId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WALLET_TX_REFUND");
            entity.HasOne(d => d.Payment).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WALLET_TX_PAYMENT");
            entity.HasOne(d => d.WalletTopUp).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WalletTopUpId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WALLET_TX_TOPUP");
            entity.HasOne(d => d.WithdrawalRequest).WithMany(p => p.WalletTransactions)
                .HasForeignKey(d => d.WithdrawalRequestId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WALLET_TX_WITHDRAWAL");
        });

        modelBuilder.Entity<WalletTopUp>(entity =>
        {
            entity.ToTable("WALLET_TOPUPS");
            entity.HasIndex(e => new { e.RequestedBy, e.IdempotencyKeyHash }, "UQ_WALLET_TOPUP_IDEMPOTENCY").IsUnique();
            entity.HasIndex(e => new { e.Status, e.CreatedAt }, "IX_WALLET_TOPUP_STATUS_CREATED");
            entity.HasIndex(e => e.GatewayTxnId, "UQ_WALLET_TOPUP_GATEWAY_TXN")
                .IsUnique()
                .HasFilter("[gateway_txn_id] IS NOT NULL");
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())").HasColumnName("id");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("amount");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.GatewayTxnId).HasMaxLength(100).HasColumnName("gateway_txn_id");
            entity.Property(e => e.ResponseCode).HasMaxLength(10).HasColumnName("response_code");
            entity.Property(e => e.TransactionStatus).HasMaxLength(10).HasColumnName("transaction_status");
            entity.Property(e => e.IdempotencyKeyHash).HasMaxLength(128).HasColumnName("idempotency_key_hash");
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("created_at");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.CompletedAt).HasColumnName("completed_at");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken().HasColumnName("row_version");
            entity.HasOne(d => d.Wallet).WithMany(p => p.TopUps)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WALLET_TOPUP_WALLET");
            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.WalletTopUps)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WALLET_TOPUP_REQUESTER");
        });

        modelBuilder.Entity<WithdrawalRequest>(entity =>
        {
            entity.ToTable("WITHDRAWAL_REQUESTS");
            entity.HasIndex(e => new { e.RequestedBy, e.IdempotencyKeyHash }, "UQ_WITHDRAW_REQUESTER_IDEMPOTENCY")
                .IsUnique()
                .HasFilter("[idempotency_key_hash] IS NOT NULL");
            entity.HasIndex(e => new { e.Status, e.RequestedAt }, "IX_WITHDRAW_STATUS_REQUESTED");
            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())").HasColumnName("id");
            entity.Property(e => e.WalletId).HasColumnName("wallet_id");
            entity.Property(e => e.RequestedBy).HasColumnName("requested_by");
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)").HasColumnName("amount");
            entity.Property(e => e.Status).HasMaxLength(20).HasColumnName("status");
            entity.Property(e => e.BankName).HasMaxLength(100).HasColumnName("bank_name");
            entity.Property(e => e.BankAccountNumber).HasMaxLength(64).HasColumnName("bank_account_number");
            entity.Property(e => e.AccountHolder).HasMaxLength(120).HasColumnName("account_holder");
            entity.Property(e => e.Note).HasMaxLength(500).HasColumnName("note");
            entity.Property(e => e.TransferReference).HasMaxLength(100).HasColumnName("transfer_reference");
            entity.Property(e => e.FailureReason).HasMaxLength(500).HasColumnName("failure_reason");
            entity.Property(e => e.IdempotencyKeyHash).HasMaxLength(128).HasColumnName("idempotency_key_hash");
            entity.Property(e => e.RequestedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("requested_at");
            entity.Property(e => e.ProcessedAt).HasColumnName("processed_at");
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("(sysdatetime())").HasColumnName("updated_at");
            entity.Property(e => e.RowVersion).IsRowVersion().IsConcurrencyToken().HasColumnName("row_version");
            entity.HasOne(d => d.Wallet).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.WalletId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_WITHDRAW_WALLET");
            entity.HasOne(d => d.RequestedByNavigation).WithMany(p => p.WithdrawalRequests)
                .HasForeignKey(d => d.RequestedBy)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_WITHDRAW_REQUESTER");
            entity.HasOne(d => d.ProcessedByNavigation).WithMany()
                .HasForeignKey(d => d.ProcessedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_WITHDRAW_PROCESSOR");
        });

        modelBuilder.Entity<Room>(entity =>
        {
            entity.ToTable("ROOMS");

            entity.HasIndex(e => e.CinemaId, "IX_ROOMS_CINEMA");

            entity.HasIndex(e => new { e.CinemaId, e.Name }, "UQ_ROOMS_NAME").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.RoomType)
                .HasMaxLength(20)
                .HasColumnName("room_type");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.TotalCapacity).HasColumnName("total_capacity");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Rooms)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ROOMS_CINEMA");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.ToTable("SEATS");

            entity.HasIndex(e => e.RoomId, "IX_SEATS_ROOM");

            entity.HasIndex(e => e.SeatTypeId, "IX_SEATS_TYPE");

            entity.HasIndex(e => new { e.RoomId, e.SeatLabel }, "UQ_SEATS_LABEL").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.ColNumber).HasColumnName("col_number");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.RowLetter)
                .HasMaxLength(1)
                .IsFixedLength()
                .HasColumnName("row_letter");
            entity.Property(e => e.SeatLabel)
                .HasMaxLength(5)
                .HasColumnName("seat_label");
            entity.Property(e => e.SeatTypeId).HasColumnName("seat_type_id");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");

            entity.HasOne(d => d.Room).WithMany(p => p.Seats)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SEATS_ROOM");

            entity.HasOne(d => d.SeatType).WithMany(p => p.Seats)
                .HasForeignKey(d => d.SeatTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SEATS_TYPE");
        });

        modelBuilder.Entity<SeatType>(entity =>
        {
            entity.ToTable("SEAT_TYPES");

            entity.HasIndex(e => e.Name, "UQ_SEAT_TYPES_NAME").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Description)
                .HasMaxLength(255)
                .HasColumnName("description");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.SeatMultiplier)
                .HasDefaultValue(1.00m)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("seat_multiplier");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
        });

        modelBuilder.Entity<Showtime>(entity =>
        {
            entity.ToTable("SHOWTIMES");

            entity.HasIndex(e => new { e.CinemaId, e.StartTime }, "IX_ST_CINEMA_START");

            entity.HasIndex(e => e.MovieId, "IX_ST_MOVIE");

            entity.HasIndex(e => e.Status, "IX_ST_STATUS");

            entity.HasIndex(e => new { e.RoomId, e.StartTime, e.EndTime }, "UX_ST_ROOM_TIME")
                .IsUnique()
                .HasFilter("([status]<>'CANCELLED')");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.EndTime).HasColumnName("end_time");
            entity.Property(e => e.LanguageType)
                .HasMaxLength(20)
                .HasColumnName("language_type");
            entity.Property(e => e.MovieId).HasColumnName("movie_id");
            entity.Property(e => e.RoomId).HasColumnName("room_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("SCHEDULED")
                .HasColumnName("status");
            entity.Property(e => e.TimeSlot)
                .HasMaxLength(20)
                .HasColumnName("time_slot");

            entity.HasOne(d => d.Cinema).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_CINEMA");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_CREATOR");

            entity.HasOne(d => d.Movie).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_MOVIE");

            entity.HasOne(d => d.Room).WithMany(p => p.Showtimes)
                .HasForeignKey(d => d.RoomId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ST_ROOM");
        });

        modelBuilder.Entity<StaffAssignment>(entity =>
        {
            entity.ToTable("STAFF_ASSIGNMENTS");

            entity.HasIndex(e => e.CinemaId, "IX_SA_CINEMA");

            entity.HasIndex(e => e.ShiftDate, "IX_SA_DATE");

            entity.HasIndex(e => e.StaffId, "IX_SA_STAFF");

            entity.HasIndex(e => new { e.StaffId, e.ShiftDate }, "UQ_SA_STAFF_DATE").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AssignedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("assigned_at");
            entity.Property(e => e.CinemaId).HasColumnName("cinema_id");
            entity.Property(e => e.ShiftDate).HasColumnName("shift_date");
            entity.Property(e => e.ShiftTime)
                .HasMaxLength(30)
                .HasColumnName("shift_time");
            entity.Property(e => e.StaffId).HasColumnName("staff_id");

            entity.HasOne(d => d.Cinema).WithMany(p => p.StaffAssignments)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SA_CINEMA");

            entity.HasOne(d => d.Staff).WithMany(p => p.StaffAssignments)
                .HasForeignKey(d => d.StaffId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SA_STAFF");
        });

        modelBuilder.Entity<Ticket>(entity =>
        {
            entity.ToTable("TICKETS");

            entity.HasIndex(e => e.BookingId, "IX_TK_BOOKING");

            entity.HasIndex(e => new { e.QrCode, e.Status }, "IX_TK_QR");

            entity.HasIndex(e => e.Status, "IX_TK_STATUS");

            entity.HasIndex(e => new { e.ExpiredAt, e.Status }, "IX_TK_EXPIRED_AT");

            entity.HasIndex(e => e.ScannedAt, "IX_TK_SCANNED_AT")
                .HasFilter("([scanned_at] IS NOT NULL)");

            entity.HasIndex(e => e.BookingSeatId, "UQ_TICKETS_BOOKING_SEAT").IsUnique();

            entity.HasIndex(e => e.QrCode, "UQ_TICKETS_QR").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.BookingId).HasColumnName("booking_id");
            entity.Property(e => e.BookingSeatId).HasColumnName("booking_seat_id");
            entity.Property(e => e.GeneratedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("generated_at");
            entity.Property(e => e.ExpiredAt).HasColumnName("expired_at");
            entity.Property(e => e.QrCode)
                .HasMaxLength(255)
                .HasColumnName("qr_code");
            entity.Property(e => e.QrPayload).HasColumnName("qr_payload");
            entity.Property(e => e.ScannedAt).HasColumnName("scanned_at");
            entity.Property(e => e.ScannedBy).HasColumnName("scanned_by");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("VALID")
                .HasColumnName("status");

            entity.Property(e => e.RowVersion)
                .IsRowVersion()
                .HasColumnName("row_version");

            entity.HasOne(d => d.Booking).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.BookingId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TK_BOOKING");

            entity.HasOne(d => d.BookingSeat).WithOne(p => p.TicketBookingSeat)
                .HasForeignKey<Ticket>(d => d.BookingSeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TK_BOOKING_SEAT");

            entity.HasOne(d => d.ScannedByNavigation).WithMany(p => p.Tickets)
                .HasForeignKey(d => d.ScannedBy)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_TK_SCANNER");

            entity.HasOne(d => d.BookingSeatNavigation).WithMany(p => p.TicketBookingSeatNavigations)
                .HasPrincipalKey(p => new { p.Id, p.BookingId })
                .HasForeignKey(d => new { d.BookingSeatId, d.BookingId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_TK_BOOKING_SEAT_BOOKING");
        });

        modelBuilder.Entity<ChatConversation>(entity =>
        {
            entity.ToTable("CHAT_CONVERSATIONS");

            entity.HasIndex(e => e.Status, "IX_CHAT_CONV_STATUS");

            entity.HasIndex(e => e.CreatedAt, "IX_CHAT_CONV_CREATED");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasColumnName("type");
            entity.Property(e => e.Title)
                .HasMaxLength(200)
                .HasColumnName("title");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.ClosedAt)
                .HasColumnName("closed_at");
        });

        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.ToTable("CHAT_PARTICIPANTS");

            entity.HasIndex(e => e.ConversationId, "IX_CHAT_PART_CONV");

            entity.HasIndex(e => e.UserId, "IX_CHAT_PART_USER");

            entity.HasIndex(e => new { e.ConversationId, e.UserId }, "UQ_CHAT_PART_CONV_USER").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasColumnName("role");
            entity.Property(e => e.JoinedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("joined_at");
            entity.Property(e => e.LastReadAt).HasColumnName("last_read_at");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Participants)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CHAT_PART_CONV");

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CHAT_PART_USER");
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.ToTable("CHAT_MESSAGES");

            entity.HasIndex(e => e.ConversationId, "IX_CHAT_MSG_CONV");

            entity.HasIndex(e => e.SenderId, "IX_CHAT_MSG_SENDER");

            entity.HasIndex(e => e.SentAt, "IX_CHAT_MSG_SENT");

            entity.HasIndex(e => new { e.ConversationId, e.SentAt }, "IX_CHAT_MSG_CONV_SENT");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.ConversationId).HasColumnName("conversation_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .HasDefaultValue("TEXT")
                .HasColumnName("type");
            entity.Property(e => e.SentAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("sent_at");
            entity.Property(e => e.ReadAt).HasColumnName("read_at");
            entity.Property(e => e.IsPinned)
                .HasDefaultValue(false)
                .HasColumnName("is_pinned");
            entity.Property(e => e.ReplyToId).HasColumnName("reply_to_id");
            entity.Property(e => e.AttachmentUrl)
                .HasMaxLength(500)
                .HasColumnName("attachment_url");
            entity.Property(e => e.AttachmentType)
                .HasMaxLength(50)
                .HasColumnName("attachment_type");

            entity.HasOne(d => d.Conversation).WithMany(p => p.Messages)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CHAT_MSG_CONV");

            entity.HasOne(d => d.Sender).WithMany()
                .HasForeignKey(d => d.SenderId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_CHAT_MSG_SENDER");

            entity.HasOne(d => d.ReplyTo).WithMany(p => p.Replies)
                .HasForeignKey(d => d.ReplyToId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("FK_CHAT_MSG_REPLY");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USERS");

            entity.HasIndex(e => e.Email, "IX_USERS_EMAIL");

            entity.HasIndex(e => e.Role, "IX_USERS_ROLE");

            entity.HasIndex(e => e.Status, "IX_USERS_STATUS");

            entity.HasIndex(e => e.Email, "UQ_USERS_EMAIL").IsUnique();

            entity.Property(e => e.Id)
                .HasDefaultValueSql("(newsequentialid())")
                .HasColumnName("id");
            entity.Property(e => e.AvatarUrl)
                .HasMaxLength(500)
                .HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.FailedLoginCount).HasColumnName("failed_login_count");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255)
                .HasColumnName("google_id");
            entity.Property(e => e.IsEmailVerified).HasColumnName("is_email_verified");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
            entity.Property(e => e.LockedUntil).HasColumnName("locked_until");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .HasColumnName("phone");
            entity.Property(e => e.Provider)
                .HasMaxLength(20)
                .HasColumnName("provider");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue("CUSTOMER")
                .HasColumnName("role");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .HasDefaultValue("ACTIVE")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("updated_at");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
