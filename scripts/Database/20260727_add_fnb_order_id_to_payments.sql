/*
    Add fnb_order_id column to PAYMENTS table
    Date: 2026-07-27

    Why this script is needed:
    - The Payment entity in EF Core includes FnbOrderId and FnbOrder navigation property
    - This allows payments to be linked to F&B orders (not just bookings)
    - The column was missing from the database, causing SQL errors: "Invalid column name 'fnb_order_id'"

    How to run:
    1. Open SQL Server Management Studio or Azure Data Studio.
    2. Select the cinema database.
    3. Run this whole script.
*/
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
GO

-- 1. Add fnb_order_id column if it doesn't exist
IF COL_LENGTH('dbo.PAYMENTS', 'fnb_order_id') IS NULL
BEGIN
    ALTER TABLE dbo.PAYMENTS ADD fnb_order_id uniqueidentifier NULL;
    PRINT 'Column fnb_order_id added to PAYMENTS table.';
END
ELSE
BEGIN
    PRINT 'Column fnb_order_id already exists in PAYMENTS table.';
END
GO

-- 2. Add index on fnb_order_id for faster lookups
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_PAY_FNB_ORDER'
      AND object_id = OBJECT_ID(N'dbo.PAYMENTS')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_PAY_FNB_ORDER
        ON dbo.PAYMENTS (fnb_order_id)
        WHERE fnb_order_id IS NOT NULL;
    PRINT 'Index IX_PAY_FNB_ORDER created on PAYMENTS table.';
END
ELSE
BEGIN
    PRINT 'Index IX_PAY_FNB_ORDER already exists on PAYMENTS table.';
END
GO

-- 3. Add foreign key constraint if it doesn't exist
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_PAY_FNB_ORDER'
      AND parent_object_id = OBJECT_ID(N'dbo.PAYMENTS')
)
BEGIN
    ALTER TABLE dbo.PAYMENTS
    ADD CONSTRAINT [FK_PAY_FNB_ORDER]
    FOREIGN KEY (fnb_order_id)
    REFERENCES dbo.FNB_ORDERS (id)
    ON DELETE SET NULL;
    PRINT 'Foreign key FK_PAY_FNB_ORDER created.';
END
ELSE
BEGIN
    PRINT 'Foreign key FK_PAY_FNB_ORDER already exists.';
END
GO

-- 4. Verify the column and FK
PRINT 'Verifying PAYMENTS fnb_order_id column...';
SELECT
    c.name AS column_name,
    TYPE_NAME(c.user_type_id) AS data_type,
    c.is_nullable,
    fk.name AS foreign_key_name,
    OBJECT_NAME(fk.parent_object_id) AS parent_table,
    OBJECT_NAME(fk.referenced_object_id) AS referenced_table
FROM sys.columns AS c
LEFT JOIN sys.foreign_keys AS fk
    ON fk.parent_object_id = c.object_id
    AND fk.parent_object_id = OBJECT_ID(N'dbo.PAYMENTS')
    AND c.name = 'fnb_order_id'
WHERE c.object_id = OBJECT_ID(N'dbo.PAYMENTS')
  AND c.name = N'fnb_order_id';
GO
