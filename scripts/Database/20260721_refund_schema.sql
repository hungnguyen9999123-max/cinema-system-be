/*
  Refund feature schema upgrade for an existing cinema database.
  Run first on a backup/test database. The script is idempotent and does not
  create test transactions or move money.
*/
SET XACT_ABORT ON;
GO

IF OBJECT_ID(N'dbo.PAYMENTS', N'U') IS NULL OR OBJECT_ID(N'dbo.REFUNDS', N'U') IS NULL
    THROW 51000, 'PAYMENTS and REFUNDS must exist before applying the refund feature patch.', 1;
GO

IF COL_LENGTH(N'dbo.PAYMENTS', N'gateway_request_at') IS NULL
    ALTER TABLE dbo.PAYMENTS ADD gateway_request_at datetime2 NULL;
GO

IF COL_LENGTH(N'dbo.REFUNDS', N'requested_by') IS NULL
    ALTER TABLE dbo.REFUNDS ADD requested_by uniqueidentifier NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'idempotency_key_hash') IS NULL
    ALTER TABLE dbo.REFUNDS ADD idempotency_key_hash nvarchar(128) NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'reason_code') IS NULL
    ALTER TABLE dbo.REFUNDS ADD reason_code nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'decision_reason') IS NULL
    ALTER TABLE dbo.REFUNDS ADD decision_reason nvarchar(500) NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'failure_code') IS NULL
    ALTER TABLE dbo.REFUNDS ADD failure_code nvarchar(50) NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'failure_message') IS NULL
    ALTER TABLE dbo.REFUNDS ADD failure_message nvarchar(500) NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'decided_at') IS NULL
    ALTER TABLE dbo.REFUNDS ADD decided_at datetime2 NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'updated_at') IS NULL
    ALTER TABLE dbo.REFUNDS ADD updated_at datetime2 NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'next_reconciliation_at') IS NULL
    ALTER TABLE dbo.REFUNDS ADD next_reconciliation_at datetime2 NULL;
IF COL_LENGTH(N'dbo.REFUNDS', N'reprocess_count') IS NULL
    ALTER TABLE dbo.REFUNDS ADD reprocess_count int NOT NULL CONSTRAINT DF_REFUNDS_REPROCESS_COUNT DEFAULT (0);
IF COL_LENGTH(N'dbo.REFUNDS', N'row_version') IS NULL
    ALTER TABLE dbo.REFUNDS ADD row_version rowversion NOT NULL;

-- The reconciliation state is longer than the legacy nvarchar(20) column.
-- The filtered active-refund index depends on status, so rebuild it after widening.
IF COL_LENGTH(N'dbo.REFUNDS', N'status') < 80
BEGIN
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REFUNDS') AND name = N'UX_REF_ACTIVE_PAYMENT')
        DROP INDEX UX_REF_ACTIVE_PAYMENT ON dbo.REFUNDS;

    ALTER TABLE dbo.REFUNDS ALTER COLUMN status nvarchar(40) NOT NULL;
END
GO

/* The legacy schema only recognizes PENDING/PROCESSED/REJECTED. Keep those
   values and add the states used by the refund workflow. */
IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.REFUNDS')
      AND name = N'CK_REF_STATUS'
)
BEGIN
    ALTER TABLE dbo.REFUNDS DROP CONSTRAINT CK_REF_STATUS;
    ALTER TABLE dbo.REFUNDS ADD CONSTRAINT CK_REF_STATUS CHECK
    (
        status IN
        (
            N'PENDING', N'PROCESSED', N'REJECTED', N'REQUESTED',
            N'PROCESSING', N'RECONCILIATION_REQUIRED', N'SUCCEEDED', N'FAILED'
        )
    );
END
GO

/* A manager approval temporarily holds the booking, then a successful
   refund moves it to REFUNDED. */
IF EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.BOOKINGS')
      AND name = N'CK_BK_STATUS'
)
BEGIN
    ALTER TABLE dbo.BOOKINGS DROP CONSTRAINT CK_BK_STATUS;
    ALTER TABLE dbo.BOOKINGS ADD CONSTRAINT CK_BK_STATUS CHECK
    (
        status IN
        (
            N'PENDING', N'PAID', N'CONFIRMED', N'CANCELLED', N'EXPIRED',
            N'REFUND_PROCESSING', N'REFUNDED'
        )
    );
END
GO

IF OBJECT_ID(N'dbo.REFUND_GATEWAY_ATTEMPTS', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.REFUND_GATEWAY_ATTEMPTS
    (
        id uniqueidentifier NOT NULL CONSTRAINT PK_REFUND_GATEWAY_ATTEMPTS PRIMARY KEY DEFAULT (newsequentialid()),
        refund_id uniqueidentifier NOT NULL,
        attempt_no int NOT NULL,
        operation nvarchar(20) NOT NULL,
        merchant_request_id nvarchar(32) NOT NULL,
        status nvarchar(30) NOT NULL,
        request_digest nvarchar(128) NULL,
        submitted_at datetime2 NOT NULL,
        responded_at datetime2 NULL,
        gateway_response_id nvarchar(32) NULL,
        gateway_transaction_no nvarchar(100) NULL,
        response_code nvarchar(10) NULL,
        transaction_status nvarchar(10) NULL,
        response_message nvarchar(500) NULL,
        CONSTRAINT FK_REF_ATTEMPT_REFUND FOREIGN KEY (refund_id) REFERENCES dbo.REFUNDS(id) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_REF_REQUESTER')
    ALTER TABLE dbo.REFUNDS ADD CONSTRAINT FK_REF_REQUESTER FOREIGN KEY (requested_by) REFERENCES dbo.USERS(id);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REFUNDS') AND name = N'UX_REF_REQUESTER_IDEMPOTENCY')
    CREATE UNIQUE INDEX UX_REF_REQUESTER_IDEMPOTENCY ON dbo.REFUNDS(requested_by, idempotency_key_hash)
    WHERE requested_by IS NOT NULL AND idempotency_key_hash IS NOT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REFUNDS') AND name = N'UX_REF_ACTIVE_PAYMENT')
    CREATE UNIQUE INDEX UX_REF_ACTIVE_PAYMENT ON dbo.REFUNDS(payment_id)
    WHERE status IN (N'REQUESTED', N'PROCESSING', N'RECONCILIATION_REQUIRED');
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REFUND_GATEWAY_ATTEMPTS') AND name = N'UQ_REF_ATTEMPT_NO')
    CREATE UNIQUE INDEX UQ_REF_ATTEMPT_NO ON dbo.REFUND_GATEWAY_ATTEMPTS(refund_id, attempt_no);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.REFUND_GATEWAY_ATTEMPTS') AND name = N'UQ_REF_ATTEMPT_REQUEST_ID')
    CREATE UNIQUE INDEX UQ_REF_ATTEMPT_REQUEST_ID ON dbo.REFUND_GATEWAY_ATTEMPTS(merchant_request_id);
GO

/* Keep the existing audit trail constraint while allowing the refund-specific
   actions emitted by RefundAuditService. */
IF OBJECT_ID(N'dbo.AUDIT_LOGS', N'U') IS NOT NULL AND EXISTS
(
    SELECT 1
    FROM sys.check_constraints
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
            N'CONFIG', N'LOGOUT', N'LOGIN', N'DELETE', N'UPDATE', N'CREATE'
        )
    );
END
GO

/* Refund notifications use an in-app record first, with email as the
   fallback channel. Preserve existing notification channels. */
IF OBJECT_ID(N'dbo.NOTIFICATIONS', N'U') IS NOT NULL AND EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.NOTIFICATIONS')
      AND name = N'CK_NOTIF_CHANNEL'
)
BEGIN
    ALTER TABLE dbo.NOTIFICATIONS DROP CONSTRAINT CK_NOTIF_CHANNEL;
    ALTER TABLE dbo.NOTIFICATIONS ADD CONSTRAINT CK_NOTIF_CHANNEL CHECK
    (
        channel IN (N'IN_APP', N'EMAIL', N'SMS', N'PUSH')
    );
END
GO

PRINT 'Refund schema patch applied. Legacy payments without gateway_request_at require manual reconciliation before refund.';
