-- Add expired_at to TICKETS for QR Ticket module
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS') AND name = N'expired_at'
)
BEGIN
    ALTER TABLE dbo.TICKETS ADD expired_at datetime2 NULL;
END
GO

UPDATE t
SET t.expired_at = s.end_time
FROM dbo.TICKETS t
INNER JOIN dbo.BOOKINGS b ON t.booking_id = b.id
INNER JOIN dbo.SHOWTIMES s ON b.showtime_id = s.id
WHERE t.expired_at IS NULL;
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS') AND name = N'expired_at'
)
AND EXISTS (SELECT 1 FROM dbo.TICKETS WHERE expired_at IS NULL)
BEGIN
    UPDATE dbo.TICKETS
    SET expired_at = DATEADD(HOUR, 3, generated_at)
    WHERE expired_at IS NULL;
END
GO

IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.TICKETS') AND name = N'expired_at'
)
AND NOT EXISTS (SELECT 1 FROM dbo.TICKETS WHERE expired_at IS NULL)
BEGIN
    ALTER TABLE dbo.TICKETS ALTER COLUMN expired_at datetime2 NOT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TK_EXPIRED_AT' AND object_id = OBJECT_ID(N'dbo.TICKETS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_EXPIRED_AT
        ON dbo.TICKETS (expired_at, status);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_TK_SCANNED_AT' AND object_id = OBJECT_ID(N'dbo.TICKETS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_TK_SCANNED_AT
        ON dbo.TICKETS (scanned_at)
        WHERE scanned_at IS NOT NULL;
END
GO
