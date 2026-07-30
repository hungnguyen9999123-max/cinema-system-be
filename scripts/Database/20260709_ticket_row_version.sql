-- Add row_version column for optimistic concurrency on TICKETS.
-- Combined with EF Core's [ConcurrencyCheck] / [Timestamp], this prevents
-- two staff from successfully scanning the same ticket at the same time.

IF COL_LENGTH('TICKETS', 'row_version') IS NULL
BEGIN
    ALTER TABLE TICKETS ADD row_version rowversion NOT NULL;
END
GO

-- Optional: index to help scan-based history queries. rowversion values are
-- already sequential per row, so an additional index is not required.