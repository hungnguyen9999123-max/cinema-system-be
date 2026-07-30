/*
    VNPAY payment / QR ticket database fix
    Date: 2026-07-15

    Why this script is needed:
    - VNPAY return marks a payment as SUCCESS.
    - The payment flow then generates QR tickets for the booking.
    - Current EF Core models expect dbo.TICKETS.expired_at and dbo.TICKETS.row_version.
    - If these columns are missing, VNPAY callback fails when loading/generating tickets.

    How to run:
    1. Open SQL Server Management Studio.
    2. Select the cinema database, for example cinema_db.
    3. Run this whole script.

    Or with sqlcmd:
    sqlcmd -S "(local)" -d cinema_db -U sa -P 12345 -C -i scripts\Database\20260715_vnpay_payment_db_fix.sql
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF COL_LENGTH('dbo.TICKETS', 'expired_at') IS NULL
BEGIN
    ALTER TABLE dbo.TICKETS ADD expired_at datetime2 NULL;
END
GO

UPDATE t
SET t.expired_at = s.end_time
FROM dbo.TICKETS AS t
INNER JOIN dbo.BOOKINGS AS b ON t.booking_id = b.id
INNER JOIN dbo.SHOWTIMES AS s ON b.showtime_id = s.id
WHERE t.expired_at IS NULL;
GO

IF COL_LENGTH('dbo.TICKETS', 'expired_at') IS NOT NULL
AND EXISTS (SELECT 1 FROM dbo.TICKETS WHERE expired_at IS NULL)
BEGIN
    UPDATE dbo.TICKETS
    SET expired_at = DATEADD(HOUR, 3, generated_at)
    WHERE expired_at IS NULL;
END
GO

IF COL_LENGTH('dbo.TICKETS', 'expired_at') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.TICKETS WHERE expired_at IS NULL)
BEGIN
    ALTER TABLE dbo.TICKETS ALTER COLUMN expired_at datetime2 NOT NULL;
END
GO

IF COL_LENGTH('dbo.TICKETS', 'row_version') IS NULL
BEGIN
    ALTER TABLE dbo.TICKETS ADD row_version rowversion NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TK_EXPIRED_AT'
      AND object_id = OBJECT_ID(N'dbo.TICKETS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_EXPIRED_AT
        ON dbo.TICKETS (expired_at, status);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_TK_SCANNED_AT'
      AND object_id = OBJECT_ID(N'dbo.TICKETS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_SCANNED_AT
        ON dbo.TICKETS (scanned_at)
        WHERE scanned_at IS NOT NULL;
END
GO

PRINT 'Verify required TICKETS columns';
SELECT
    c.name AS column_name,
    TYPE_NAME(c.user_type_id) AS data_type,
    c.is_nullable
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID(N'dbo.TICKETS')
  AND c.name IN (N'expired_at', N'row_version')
ORDER BY c.name;

PRINT 'Verify required TICKETS indexes';
SELECT
    i.name AS index_name
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.TICKETS')
  AND i.name IN (N'IX_TK_EXPIRED_AT', N'IX_TK_SCANNED_AT');
GO
