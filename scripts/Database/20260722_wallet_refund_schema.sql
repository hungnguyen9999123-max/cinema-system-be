/*
  Wallet-based refund and manual bank-withdrawal upgrade.
  Refund approvals credit the customer's internal wallet. A withdrawal reserves
  that balance until a Manager records a bank-transfer reference or rejects it.
  The script is idempotent and preserves existing VNPAY refund history.
*/
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.USERS', N'U') IS NULL OR OBJECT_ID(N'dbo.REFUNDS', N'U') IS NULL
    THROW 51000, 'USERS and REFUNDS must exist before applying the wallet refund patch.', 1;
GO

IF OBJECT_ID(N'dbo.WALLETS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WALLETS
    (
        id uniqueidentifier NOT NULL CONSTRAINT PK_WALLETS PRIMARY KEY DEFAULT (newsequentialid()),
        user_id uniqueidentifier NOT NULL,
        balance decimal(18, 2) NOT NULL CONSTRAINT DF_WALLETS_BALANCE DEFAULT (0),
        created_at datetime2 NOT NULL CONSTRAINT DF_WALLETS_CREATED DEFAULT (sysdatetime()),
        updated_at datetime2 NOT NULL CONSTRAINT DF_WALLETS_UPDATED DEFAULT (sysdatetime()),
        row_version rowversion NOT NULL,
        CONSTRAINT UQ_WALLETS_USER UNIQUE (user_id),
        CONSTRAINT CK_WALLETS_BALANCE CHECK (balance >= 0),
        CONSTRAINT FK_WALLETS_USERS FOREIGN KEY (user_id) REFERENCES dbo.USERS(id) ON DELETE CASCADE
    );
END
GO

IF OBJECT_ID(N'dbo.WITHDRAWAL_REQUESTS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WITHDRAWAL_REQUESTS
    (
        id uniqueidentifier NOT NULL CONSTRAINT PK_WITHDRAWAL_REQUESTS PRIMARY KEY DEFAULT (newsequentialid()),
        wallet_id uniqueidentifier NOT NULL,
        requested_by uniqueidentifier NOT NULL,
        processed_by uniqueidentifier NULL,
        amount decimal(18, 2) NOT NULL,
        status nvarchar(20) NOT NULL CONSTRAINT DF_WITHDRAW_STATUS DEFAULT (N'PENDING'),
        bank_name nvarchar(100) NOT NULL,
        bank_account_number nvarchar(64) NOT NULL,
        account_holder nvarchar(120) NOT NULL,
        note nvarchar(500) NULL,
        transfer_reference nvarchar(100) NULL,
        failure_reason nvarchar(500) NULL,
        idempotency_key_hash nvarchar(128) NULL,
        requested_at datetime2 NOT NULL CONSTRAINT DF_WITHDRAW_REQUESTED DEFAULT (sysdatetime()),
        processed_at datetime2 NULL,
        updated_at datetime2 NOT NULL CONSTRAINT DF_WITHDRAW_UPDATED DEFAULT (sysdatetime()),
        row_version rowversion NOT NULL,
        CONSTRAINT CK_WITHDRAW_AMOUNT CHECK (amount > 0),
        CONSTRAINT CK_WITHDRAW_STATUS CHECK (status IN (N'PENDING', N'COMPLETED', N'REJECTED')),
        CONSTRAINT FK_WITHDRAW_WALLET FOREIGN KEY (wallet_id) REFERENCES dbo.WALLETS(id) ON DELETE CASCADE,
        CONSTRAINT FK_WITHDRAW_REQUESTER FOREIGN KEY (requested_by) REFERENCES dbo.USERS(id),
        CONSTRAINT FK_WITHDRAW_PROCESSOR FOREIGN KEY (processed_by) REFERENCES dbo.USERS(id)
    );
END
GO

IF OBJECT_ID(N'dbo.WALLET_TRANSACTIONS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WALLET_TRANSACTIONS
    (
        id uniqueidentifier NOT NULL CONSTRAINT PK_WALLET_TRANSACTIONS PRIMARY KEY DEFAULT (newsequentialid()),
        wallet_id uniqueidentifier NOT NULL,
        refund_id uniqueidentifier NULL,
        withdrawal_request_id uniqueidentifier NULL,
        type nvarchar(30) NOT NULL,
        amount decimal(18, 2) NOT NULL,
        balance_after decimal(18, 2) NOT NULL,
        description nvarchar(500) NOT NULL,
        created_at datetime2 NOT NULL CONSTRAINT DF_WALLET_TX_CREATED DEFAULT (sysdatetime()),
        CONSTRAINT CK_WALLET_TX_TYPE CHECK (type IN (N'REFUND_CREDIT', N'WITHDRAWAL_HOLD', N'WITHDRAWAL_REVERSAL')),
        CONSTRAINT CK_WALLET_TX_AMOUNT CHECK (amount <> 0),
        CONSTRAINT CK_WALLET_TX_BALANCE CHECK (balance_after >= 0),
        CONSTRAINT FK_WALLET_TX_WALLET FOREIGN KEY (wallet_id) REFERENCES dbo.WALLETS(id) ON DELETE CASCADE,
        CONSTRAINT FK_WALLET_TX_REFUND FOREIGN KEY (refund_id) REFERENCES dbo.REFUNDS(id),
        CONSTRAINT FK_WALLET_TX_WITHDRAWAL FOREIGN KEY (withdrawal_request_id) REFERENCES dbo.WITHDRAWAL_REQUESTS(id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WALLET_TRANSACTIONS') AND name = N'IX_WALLET_TX_WALLET_CREATED')
    CREATE INDEX IX_WALLET_TX_WALLET_CREATED ON dbo.WALLET_TRANSACTIONS(wallet_id, created_at DESC);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WALLET_TRANSACTIONS') AND name = N'UQ_WALLET_TX_REFUND')
    CREATE UNIQUE INDEX UQ_WALLET_TX_REFUND ON dbo.WALLET_TRANSACTIONS(refund_id) WHERE refund_id IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WITHDRAWAL_REQUESTS') AND name = N'UQ_WITHDRAW_REQUESTER_IDEMPOTENCY')
    CREATE UNIQUE INDEX UQ_WITHDRAW_REQUESTER_IDEMPOTENCY ON dbo.WITHDRAWAL_REQUESTS(requested_by, idempotency_key_hash) WHERE idempotency_key_hash IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.WITHDRAWAL_REQUESTS') AND name = N'IX_WITHDRAW_STATUS_REQUESTED')
    CREATE INDEX IX_WITHDRAW_STATUS_REQUESTED ON dbo.WITHDRAWAL_REQUESTS(status, requested_at DESC);
GO

IF OBJECT_ID(N'dbo.AUDIT_LOGS', N'U') IS NOT NULL AND EXISTS
(
    SELECT 1 FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.AUDIT_LOGS')
      AND name = N'CK_AL_ACTION'
)
BEGIN
    ALTER TABLE dbo.AUDIT_LOGS DROP CONSTRAINT CK_AL_ACTION;
    ALTER TABLE dbo.AUDIT_LOGS ADD CONSTRAINT CK_AL_ACTION CHECK
    (
        action_type IN
        (
            N'REFUND', N'REFUND_CREATE', N'REFUND_AUTO_CREDIT', N'REFUND_VIEW_OWN', N'REFUND_VIEW_OPS',
            N'REFUND_APPROVE', N'REFUND_REJECT', N'REFUND_REPROCESS',
            N'WALLET_VIEW', N'WITHDRAW_CREATE', N'WITHDRAW_VIEW_OWN',
            N'WITHDRAW_VIEW_OPS', N'WITHDRAW_COMPLETE', N'WITHDRAW_REJECT',
            N'CONFIG', N'LOGOUT', N'LOGIN', N'DELETE', N'UPDATE', N'CREATE'
        )
    );
END
GO

PRINT 'Wallet refund schema patch applied.';
