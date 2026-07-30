/*
    Fix-up / complete migration after partial run.
    Safe to re-run when room_type_id/time_slot_id exist but legacy columns remain.
*/

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('PRICING_RULES', 'room_type_id') IS NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES ADD room_type_id INT NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'time_slot_id') IS NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES ADD time_slot_id INT NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'room_type') IS NOT NULL
BEGIN
    UPDATE dbo.PRICING_RULES
    SET room_type_id = CASE UPPER(LTRIM(RTRIM(room_type)))
        WHEN N'STANDARD' THEN 1
        WHEN N'VIP' THEN 2
        WHEN N'IMAX' THEN 3
        WHEN N'4DX' THEN 4
        ELSE NULL
    END
    WHERE room_type_id IS NULL;
END;
GO

IF COL_LENGTH('PRICING_RULES', 'time_slot') IS NOT NULL
BEGIN
    UPDATE dbo.PRICING_RULES
    SET time_slot_id = CASE UPPER(LTRIM(RTRIM(time_slot)))
        WHEN N'PEAK' THEN 2
        WHEN N'MORNING' THEN 1
        WHEN N'AFTERNOON' THEN 1
        WHEN N'EVENING' THEN 1
        WHEN N'MIDNIGHT' THEN 1
        ELSE NULL
    END
    WHERE time_slot_id IS NULL;
END;
GO

IF EXISTS (
    SELECT 1
    FROM dbo.PRICING_RULES
    WHERE room_type_id IS NULL OR time_slot_id IS NULL
)
BEGIN
    RAISERROR('Migration failed: unmapped room_type/time_slot values exist in PRICING_RULES.', 16, 1);
END;
GO

SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PR_ROOM_TYPE' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP CONSTRAINT CK_PR_ROOM_TYPE;
END;

IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PR_TIME_SLOT' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP CONSTRAINT CK_PR_TIME_SLOT;
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

IF COL_LENGTH('PRICING_RULES', 'room_type') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP COLUMN room_type;
END;

IF COL_LENGTH('PRICING_RULES', 'time_slot') IS NOT NULL
BEGIN
    ALTER TABLE dbo.PRICING_RULES DROP COLUMN time_slot;
END;

ALTER TABLE dbo.PRICING_RULES ALTER COLUMN room_type_id INT NOT NULL;
ALTER TABLE dbo.PRICING_RULES ALTER COLUMN time_slot_id INT NOT NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PRICING_RULES_room_type_id' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES
    ADD CONSTRAINT CK_PRICING_RULES_room_type_id CHECK (room_type_id IN (1, 2, 3, 4));
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = N'CK_PRICING_RULES_time_slot_id' AND parent_object_id = OBJECT_ID(N'dbo.PRICING_RULES')
)
BEGIN
    ALTER TABLE dbo.PRICING_RULES
    ADD CONSTRAINT CK_PRICING_RULES_time_slot_id CHECK (time_slot_id IN (1, 2));
END;

CREATE UNIQUE NONCLUSTERED INDEX UX_PR_ACTIVE_COMBO_DATES
    ON dbo.PRICING_RULES (cinema_id, room_type_id, time_slot_id, effective_from, effective_to)
    WHERE is_active = 1;

CREATE NONCLUSTERED INDEX IX_PR_LOOKUP
    ON dbo.PRICING_RULES (cinema_id, room_type_id, time_slot_id, is_active, effective_from, effective_to, base_price, time_multiplier);

COMMIT TRANSACTION;
GO

PRINT 'Migration completed: PRICING_RULES now uses room_type_id and time_slot_id.';
GO
