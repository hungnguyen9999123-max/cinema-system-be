using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Refunds;
using CinemaSystem.Common.DTOs.Wallets;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.DAL.Repository.Refunds;
using CinemaSystem.Services.Services.Refunds;
using CinemaSystem.Services.Services.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CinemaSystem.RefundFlowTests;

public sealed class RefundServiceFlowTests
{
    [Fact]
    public async Task RefundRepository_Update_KeepsNewRefundInAddedState()
    {
        using var dbContext = new CinemaDbContext();
        var repository = new RefundRepository(dbContext);
        var refund = new Refund
        {
            Id = Guid.NewGuid(),
            PaymentId = Guid.NewGuid(),
            RefundAmount = 100_000m,
            Status = RefundStatus.Requested,
            RequestedAt = DateTime.UtcNow
        };

        await repository.AddAsync(refund);
        repository.Update(refund);

        Assert.Equal(EntityState.Added, dbContext.Entry(refund).State);
    }

    [Fact]
    public async Task ApproveAsync_CreditsWalletAndCancelsBookingAssetsAtomically()
    {
        var fixture = RefundFixture.Create(RefundStatus.Requested, "CONFIRMED");
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        var result = await service.ApproveAsync(fixture.Refund.Id, Guid.NewGuid(), "Wallet refund");

        Assert.Equal(RefundStatus.Succeeded, result.Status);
        Assert.Equal(RefundStatus.Succeeded, fixture.Refund.Status);
        Assert.Equal("REFUNDED", fixture.Booking.Status);
        Assert.Equal(TicketStatus.Cancelled, fixture.Ticket.Status);
        Assert.Equal("RELEASED", fixture.BookingSeat.SeatStatus);
        Assert.NotNull(walletRepository.Wallet);
        Assert.Equal(fixture.Refund.RefundAmount, walletRepository.Wallet!.Balance);
        var transaction = Assert.Single(walletRepository.Transactions);
        Assert.Equal(WalletTransactionType.RefundCredit, transaction.Type);
        Assert.Equal(fixture.Refund.Id, transaction.RefundId);
        Assert.Equal(fixture.Refund.RefundAmount, transaction.BalanceAfter);
    }

    [Fact]
    public async Task ApproveAsync_CreditsWalletForRefundProcessingBooking()
    {
        var fixture = RefundFixture.Create(RefundStatus.ReconciliationRequired, "REFUND_PROCESSING");
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        await service.ApproveAsync(fixture.Refund.Id, Guid.NewGuid(), null);

        Assert.Equal(RefundStatus.Succeeded, fixture.Refund.Status);
        Assert.Equal("REFUNDED", fixture.Booking.Status);
        Assert.Single(walletRepository.Transactions);
        Assert.Equal(WalletTransactionType.RefundCredit, walletRepository.Transactions[0].Type);
    }

    [Fact]
    public async Task ApproveAsync_CreditsWalletForVnPayBooking()
    {
        var fixture = RefundFixture.Create(RefundStatus.Requested, "CONFIRMED", gateway: "VNPAY");
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        var result = await service.ApproveAsync(fixture.Refund.Id, Guid.NewGuid(), null);

        Assert.Equal(RefundStatus.Succeeded, result.Status);
        Assert.Equal("REFUNDED", fixture.Booking.Status);
        Assert.Equal(fixture.Refund.RefundAmount, walletRepository.Wallet!.Balance);
        Assert.Equal(WalletTransactionType.RefundCredit, Assert.Single(walletRepository.Transactions).Type);
    }

    [Fact]
    public async Task CreateAsync_WhenRefundMeetsPolicy_CreditsWalletAndCancelsBookingAutomatically()
    {
        var fixture = RefundFixture.Create(RefundStatus.Succeeded, "CONFIRMED");
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        var result = await service.CreateAsync(
            fixture.Booking.CustomerId,
            Guid.NewGuid().ToString(),
            new CreateRefundRequestDto
            {
                BookingId = fixture.Booking.Id,
                ReasonCode = "PLAN_CHANGED"
            });

        Assert.Equal(RefundStatus.Succeeded, result.Status);
        Assert.Equal("REFUNDED", fixture.Booking.Status);
        Assert.Equal(TicketStatus.Cancelled, fixture.Ticket.Status);
        Assert.Equal("RELEASED", fixture.BookingSeat.SeatStatus);
        Assert.NotNull(walletRepository.Wallet);
        Assert.Equal(100_000m, walletRepository.Wallet!.Balance);
        Assert.Equal(WalletTransactionType.RefundCredit, Assert.Single(walletRepository.Transactions).Type);
    }

    [Fact]
    public async Task CreateAsync_WhenRefundDoesNotMeetPolicy_DoesNotCreditWallet()
    {
        var fixture = RefundFixture.Create(RefundStatus.Succeeded, "CONFIRMED");
        fixture.Booking.Showtime.StartTime = DateTime.UtcNow.AddMinutes(119);
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        await Assert.ThrowsAsync<BusinessConflictException>(async () => await service.CreateAsync(
            fixture.Booking.CustomerId,
            Guid.NewGuid().ToString(),
            new CreateRefundRequestDto
            {
                BookingId = fixture.Booking.Id,
                ReasonCode = "PLAN_CHANGED"
            }));

        Assert.Null(walletRepository.Wallet);
        Assert.Equal("CONFIRMED", fixture.Booking.Status);
        Assert.Equal(TicketStatus.Valid, fixture.Ticket.Status);
        Assert.Equal("BOOKED", fixture.BookingSeat.SeatStatus);
    }

    [Fact]
    public async Task CreateAsync_WhenTicketWasPurchasedMoreThanTwelveHoursAgo_DoesNotCreditWallet()
    {
        var fixture = RefundFixture.Create(RefundStatus.Succeeded, "CONFIRMED");
        fixture.Refund.Payment.PaidAt = DateTime.UtcNow.AddHours(-12).AddMinutes(-1);
        var walletRepository = new InMemoryWalletRepository();
        var service = CreateRefundService(fixture.Repository, walletRepository);

        await Assert.ThrowsAsync<BusinessConflictException>(async () => await service.CreateAsync(
            fixture.Booking.CustomerId,
            Guid.NewGuid().ToString(),
            new CreateRefundRequestDto
            {
                BookingId = fixture.Booking.Id,
                ReasonCode = "PLAN_CHANGED"
            }));

        Assert.Null(walletRepository.Wallet);
        Assert.Equal("CONFIRMED", fixture.Booking.Status);
        Assert.Equal(TicketStatus.Valid, fixture.Ticket.Status);
        Assert.Equal("BOOKED", fixture.BookingSeat.SeatStatus);
    }

    [Fact]
    public void GetPolicy_ExposesTwoHourAndTwelveHourLimits()
    {
        var fixture = RefundFixture.Create(RefundStatus.Succeeded, "CONFIRMED");
        var service = CreateRefundService(fixture.Repository, new InMemoryWalletRepository());

        var policy = service.GetPolicy();

        Assert.Equal(120, policy.CutoffMinutes);
        Assert.Equal(12, policy.MaxHoursAfterPurchase);
    }

    [Fact]
    public async Task WithdrawalReject_RestoresReservedBalance()
    {
        var customerId = Guid.NewGuid();
        var walletRepository = new InMemoryWalletRepository(new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = customerId,
            Balance = 100_000m,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var walletService = new WalletService(walletRepository, new NoOpUnitOfWork());

        var withdrawal = await walletService.CreateWithdrawalAsync(
            customerId,
            Guid.NewGuid().ToString(),
            new CreateWithdrawalRequestDto
            {
                Amount = 60_000m,
                BankName = "Demo Bank",
                BankAccountNumber = "1234567890",
                AccountHolder = "Test Customer"
            });

        Assert.Equal(WithdrawalStatus.Pending, withdrawal.Status);
        Assert.Equal(40_000m, walletRepository.Wallet!.Balance);
        Assert.Single(walletRepository.Transactions);
        Assert.Equal(WalletTransactionType.WithdrawalHold, walletRepository.Transactions[0].Type);

        var resolved = await walletService.RejectWithdrawalAsync(
            withdrawal.WithdrawalId,
            Guid.NewGuid(),
            new WithdrawalDecisionDto { InternalNote = "Không thể xác minh thông tin nhận tiền" });

        Assert.Equal(WithdrawalStatus.Rejected, resolved.Status);
        Assert.Equal(100_000m, walletRepository.Wallet.Balance);
        Assert.Equal(2, walletRepository.Transactions.Count);
        Assert.Equal(WalletTransactionType.WithdrawalReversal, walletRepository.Transactions[1].Type);
    }

    private static RefundService CreateRefundService(IRefundRepository refundRepository, IWalletRepository walletRepository) =>
        new(
            refundRepository,
            walletRepository,
            new NoOpUnitOfWork(),
            new NoOpNotificationService(),
            new ConfigurationBuilder().Build(),
            NullLogger<RefundService>.Instance);

    private sealed class InMemoryRefundRepository(Refund refund) : IRefundRepository
    {
        public Task<Payment?> GetPaymentForRefundAsync(Guid bookingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Payment?>(refund.Payment.BookingId == bookingId ? refund.Payment : null);
        public Task<Refund?> GetByIdAsync(Guid refundId, CancellationToken cancellationToken = default) => Task.FromResult<Refund?>(refund.Id == refundId ? refund : null);
        public Task<Refund?> GetActiveForPaymentAsync(Guid paymentId, CancellationToken cancellationToken = default) => Task.FromResult<Refund?>(null);
        public Task<Refund?> GetByIdempotencyKeyAsync(Guid customerId, string keyHash, CancellationToken cancellationToken = default) => Task.FromResult<Refund?>(null);
        public Task<int> CountRequestsByCustomerSinceAsync(Guid customerId, DateTime since, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetByCustomerAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<Refund>)Array.Empty<Refund>(), 0));
        public Task<(IReadOnlyList<Refund> Items, int TotalCount)> GetForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<Refund>)Array.Empty<Refund>(), 0));
        public Task<IReadOnlyList<Refund>> GetDueForProcessingAsync(DateTime now, CancellationToken cancellationToken = default) => Task.FromResult((IReadOnlyList<Refund>)Array.Empty<Refund>());
        public Task AddAsync(Refund item, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task AddAttemptAsync(RefundGatewayAttempt attempt, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Update(Refund item) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class InMemoryWalletRepository(Wallet? wallet = null) : IWalletRepository
    {
        public Wallet? Wallet { get; private set; } = wallet;
        public List<WalletTransaction> Transactions { get; } = [];
        public List<WithdrawalRequest> Withdrawals { get; } = [];
        public List<WalletTopUp> TopUps { get; } = [];

        public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(Wallet?.UserId == userId ? Wallet : null);
        public Task<Wallet> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Wallet ??= new Wallet { Id = Guid.NewGuid(), UserId = userId, Balance = 0m, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            return Task.FromResult(Wallet);
        }

        public Task<(Guid WalletId, decimal BalanceAfter)?> TryDebitAsync(Guid userId, decimal amount, DateTime updatedAt, CancellationToken cancellationToken = default)
        {
            if (Wallet is null || Wallet.UserId != userId || Wallet.Balance < amount)
                return Task.FromResult<(Guid WalletId, decimal BalanceAfter)?>(null);
            Wallet.Balance -= amount;
            Wallet.UpdatedAt = updatedAt;
            return Task.FromResult<(Guid WalletId, decimal BalanceAfter)?>(new(Wallet.Id, Wallet.Balance));
        }
        public Task<bool> HasRefundCreditAsync(Guid refundId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.Any(transaction => transaction.RefundId == refundId));
        public Task<bool> HasTopUpCreditAsync(Guid topUpId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.Any(transaction => transaction.WalletTopUpId == topUpId));
        public Task<bool> HasBookingPaymentDebitAsync(Guid paymentId, CancellationToken cancellationToken = default) => Task.FromResult(Transactions.Any(transaction => transaction.PaymentId == paymentId));
        public Task AddTransactionAsync(WalletTransaction transaction, CancellationToken cancellationToken = default)
        {
            Transactions.Add(transaction);
            return Task.CompletedTask;
        }
        public Task<(IReadOnlyList<WalletTransaction> Items, int TotalCount)> GetTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<WalletTransaction>)Transactions, Transactions.Count));
        public Task<WithdrawalRequest?> GetWithdrawalByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default) => Task.FromResult<WithdrawalRequest?>(Withdrawals.FirstOrDefault(withdrawal => withdrawal.Id == withdrawalId));
        public Task<WithdrawalRequest?> GetWithdrawalByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default) => Task.FromResult<WithdrawalRequest?>(Withdrawals.FirstOrDefault(withdrawal => withdrawal.RequestedBy == userId && withdrawal.IdempotencyKeyHash == keyHash));
        public Task AddWithdrawalAsync(WithdrawalRequest withdrawal, CancellationToken cancellationToken = default)
        {
            Withdrawals.Add(withdrawal);
            return Task.CompletedTask;
        }
        public Task<WalletTopUp?> GetTopUpByIdAsync(Guid topUpId, CancellationToken cancellationToken = default) => Task.FromResult<WalletTopUp?>(TopUps.FirstOrDefault(topUp => topUp.Id == topUpId));
        public Task<WalletTopUp?> GetTopUpByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default) => Task.FromResult<WalletTopUp?>(TopUps.FirstOrDefault(topUp => topUp.RequestedBy == userId && topUp.IdempotencyKeyHash == keyHash));
        public Task<WalletTopUp?> GetTopUpByGatewayTxnIdAsync(string gatewayTxnId, CancellationToken cancellationToken = default) => Task.FromResult<WalletTopUp?>(TopUps.FirstOrDefault(topUp => topUp.GatewayTxnId == gatewayTxnId));
        public Task AddTopUpAsync(WalletTopUp topUp, CancellationToken cancellationToken = default)
        {
            TopUps.Add(topUp);
            return Task.CompletedTask;
        }
        public Task<int> ExpirePendingTopUpsAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default)
        {
            var expired = TopUps.Where(topUp => topUp.RequestedBy == userId && topUp.Status == "PENDING" && topUp.ExpiresAt <= now).ToList();
            foreach (var topUp in expired)
            {
                topUp.Status = "EXPIRED";
                topUp.CompletedAt = topUp.ExpiresAt;
            }
            return Task.FromResult(expired.Count);
        }
        public Task<(IReadOnlyList<WalletTopUp> Items, int TotalCount)> GetTopUpsForCustomerAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<WalletTopUp>)TopUps.Where(topUp => topUp.RequestedBy == userId).ToList(), TopUps.Count(topUp => topUp.RequestedBy == userId)));
        public Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForCustomerAsync(Guid userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<WithdrawalRequest>)Withdrawals, Withdrawals.Count));
        public Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(((IReadOnlyList<WithdrawalRequest>)Withdrawals, Withdrawals.Count));
        public void Update(Wallet wallet) { }
        public void Update(WithdrawalRequest withdrawal) { }
        public void Update(WalletTopUp topUp) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class NoOpUnitOfWork : IUnitOfWork
    {
        public Task BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackTransactionAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default) => action(cancellationToken);
        public void Dispose() { }
    }

    private sealed class NoOpNotificationService : IRefundNotificationService
    {
        public Task NotifyCustomerAsync(Refund refund, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed record RefundFixture(Refund Refund, Booking Booking, Ticket Ticket, BookingSeat BookingSeat, InMemoryRefundRepository Repository)
    {
        public static RefundFixture Create(string refundStatus, string bookingStatus, string gateway = "WALLET")
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = Guid.NewGuid(),
                BookingRef = "WALLET-REFUND-TEST",
                Status = bookingStatus,
                BookedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10),
                Showtime = new Showtime { StartTime = DateTime.UtcNow.AddDays(2) }
            };
            var bookingSeat = new BookingSeat { Id = Guid.NewGuid(), BookingId = booking.Id, SeatStatus = "BOOKED" };
            var ticket = new Ticket { Id = Guid.NewGuid(), BookingId = booking.Id, BookingSeatId = bookingSeat.Id, QrCode = "test", QrPayload = "test", Status = TicketStatus.Valid, GeneratedAt = DateTime.UtcNow, ExpiredAt = DateTime.UtcNow.AddDays(2) };
            booking.BookingSeatBookings.Add(bookingSeat);
            booking.Tickets.Add(ticket);
            var payment = new Payment { Id = Guid.NewGuid(), BookingId = booking.Id, Gateway = gateway, Status = "SUCCESS", Amount = 100_000m, PaidAt = DateTime.UtcNow, CreatedAt = DateTime.UtcNow, Booking = booking };
            var refund = new Refund { Id = Guid.NewGuid(), PaymentId = payment.Id, Payment = payment, RequestedBy = booking.CustomerId, RefundAmount = payment.Amount, Status = refundStatus, RequestedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            return new RefundFixture(refund, booking, ticket, bookingSeat, new InMemoryRefundRepository(refund));
        }
    }
}
