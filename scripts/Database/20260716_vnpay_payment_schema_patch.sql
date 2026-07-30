/*
    VNPAY payment schema patch
    Date: 2026-07-16

    Purpose:
    - Run this on an existing cinema database.
    - This script does not create the database from scratch.
    - This script does not insert demo/sample data.
    - It only adds the database structure required by the updated VNPAY
      payment callback and QR ticket flow.

    What it changes:
    - Adds dbo.TICKETS.expired_at if missing.
    - Backfills expired_at for existing tickets so the column can be NOT NULL.
      This is a migration step for existing production/test rows, not sample data.
    - Adds dbo.TICKETS.row_version if missing.
    - Adds indexes used by QR ticket expiry/scanning queries if missing.

    How to run:
    - In SSMS: choose the existing cinema database, then run this whole file.
    - With sqlcmd:
      sqlcmd -S "(local)" -d cinema_db -U sa -P 12345 -C -i scripts\Database\20260716_vnpay_payment_schema_patch.sql
*/

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

IF OBJECT_ID(N'dbo.TICKETS', N'U') IS NULL
BEGIN
    THROW 51000, 'dbo.TICKETS table was not found. Run this script on the existing cinema database.', 1;
END
GO

IF COL_LENGTH(N'dbo.TICKETS', N'expired_at') IS NULL
BEGIN
    ALTER TABLE dbo.TICKETS ADD expired_at datetime2 NULL;
END
GO

IF COL_LENGTH(N'dbo.TICKETS', N'expired_at') IS NOT NULL
BEGIN
    UPDATE t
    SET t.expired_at = s.end_time
    FROM dbo.TICKETS AS t
    INNER JOIN dbo.BOOKINGS AS b ON t.booking_id = b.id
    INNER JOIN dbo.SHOWTIMES AS s ON b.showtime_id = s.id
    WHERE t.expired_at IS NULL;
END
GO

IF COL_LENGTH(N'dbo.TICKETS', N'expired_at') IS NOT NULL
BEGIN
    UPDATE dbo.TICKETS
    SET expired_at = DATEADD(HOUR, 3, generated_at)
    WHERE expired_at IS NULL
      AND generated_at IS NOT NULL;
END
GO

IF COL_LENGTH(N'dbo.TICKETS', N'expired_at') IS NOT NULL
AND NOT EXISTS (SELECT 1 FROM dbo.TICKETS WHERE expired_at IS NULL)
AND EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS')
      AND name = N'expired_at'
      AND is_nullable = 1
)
BEGIN
    ALTER TABLE dbo.TICKETS ALTER COLUMN expired_at datetime2 NOT NULL;
END
GO

IF COL_LENGTH(N'dbo.TICKETS', N'row_version') IS NULL
BEGIN
    ALTER TABLE dbo.TICKETS ADD row_version rowversion NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS')
      AND name = N'IX_TK_EXPIRED_AT'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_EXPIRED_AT
        ON dbo.TICKETS (expired_at, status);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS')
      AND name = N'IX_TK_SCANNED_AT'
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_SCANNED_AT
        ON dbo.TICKETS (scanned_at)
        WHERE scanned_at IS NOT NULL;
END
GO

PRINT 'VNPAY payment schema patch verification';

SELECT
    c.name AS column_name,
    TYPE_NAME(c.user_type_id) AS data_type,
    c.is_nullable
FROM sys.columns AS c
WHERE c.object_id = OBJECT_ID(N'dbo.TICKETS')
  AND c.name IN (N'expired_at', N'row_version')
ORDER BY c.name;

SELECT
    i.name AS index_name
FROM sys.indexes AS i
WHERE i.object_id = OBJECT_ID(N'dbo.TICKETS')
  AND i.name IN (N'IX_TK_EXPIRED_AT', N'IX_TK_SCANNED_AT')
ORDER BY i.name;
GO
