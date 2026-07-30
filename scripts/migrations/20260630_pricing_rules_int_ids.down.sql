/*
    Rollback: PRICING_RULES int IDs -> string columns
    Date: 2026-06-30
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('PRICING_RULES', 'room_type') IS NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES ADD room_type NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'time_slot') IS NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES ADD time_slot NVARCHAR(20) NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'room_type_id') IS NOT NULL
BEGIN
    UPDATE dbo.PRICING_RULES
    SET room_type = CASE room_type_id
        WHEN 1 THEN N'STANDARD'
        WHEN 2 THEN N'VIP'
        WHEN 3 THEN N'IMAX'
        WHEN 4 THEN N'4DX'
        ELSE NULL
    END
    WHERE room_type IS NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'time_slot_id') IS NOT NULL
BEGIN
    UPDATE dbo.PRICING_RULES
    SET time_slot = CASE time_slot_id
        WHEN 1 THEN N'MORNING'
        WHEN 2 THEN N'PEAK'
        ELSE NULL
    END
    WHERE time_slot IS NULL;
END;
GO

IF EXISTS (
    SELECT 1 FROM dbo.PRICING_RULES WHERE room_type IS NULL OR time_slot IS NULL
)
BEGIN
    RAISERROR('Rollback failed: unmapped room_type_id/time_slot_id values exist.', 16, 1);
END;
GO

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PRICING_RULES_room_type_id' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP CONSTRAINT CK_PRICING_RULES_room_type_id;
END;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PRICING_RULES_time_slot_id' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP CONSTRAINT CK_PRICING_RULES_time_slot_id;
END;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PRICING_RULES') AND name = N'UX_PR_ACTIVE_COMBO_DATES'
)
BEGIN
    DROP INDEX UX_PR_ACTIVE_COMBO_DATES ON dbo.PRICING_RULES;
END;

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.PRICING_RULES') AND name = N'IX_PR_LOOKUP'
)
BEGIN
    DROP INDEX IX_PR_LOOKUP ON dbo.PRICING_RULES;
END;

IF COL_LENGTH('PRICING_RULES', 'room_type_id') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP COLUMN room_type_id;
END;

IF COL_LENGTH('PRICING_RULES', 'time_slot_id') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP COLUMN time_slot_id;
END;

ALTER TABLE dbo.PRICING_RULES ALTER COLUMN room_type NVARCHAR(20) NOT NULL;
ALTER TABLE dbo.PRICING_RULES ALTER COLUMN time_slot NVARCHAR(20) NOT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PR_ROOM_TYPE' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES
    ADD CONSTRAINT CK_PR_ROOM_TYPE
        CHECK (room_type IN (N'STANDARD', N'VIP', N'IMAX', N'4DX'));
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PR_TIME_SLOT' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES
    ADD CONSTRAINT CK_PR_TIME_SLOT
        CHECK (time_slot IN (N'MORNING', N'AFTERNOON', N'EVENING', N'MIDNIGHT', N'PEAK'));
END;

CREATE UNIQUE NONCLUSTERED INDEX UX_PR_ACTIVE_COMBO_DATES
    ON dbo.PRICING_RULES (cinema_id, room_type, time_slot, effective_from, effective_to)
    WHERE is_active = 1;

CREATE NONCLUSTERED INDEX IX_PR_LOOKUP
    ON dbo.PRICING_RULES (cinema_id, room_type, time_slot, is_active, effective_from, effective_to, base_price, time_multiplier);

COMMIT TRANSACTION;
GO

PRINT 'Rollback completed: PRICING_RULES restored to room_type and time_slot.';
GO
