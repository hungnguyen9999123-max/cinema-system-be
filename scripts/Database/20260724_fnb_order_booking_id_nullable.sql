-- Migration: Fix F&B Orders schema for counter orders (F&B POS)
-- Date: 2026-07-24
-- Description: Counter orders (F&B POS) need nullable booking_id and proper status values

-- 1. Allow NULL booking_id for counter orders
ALTER TABLE [FNB_ORDERS] ALTER COLUMN [booking_id] UNIQUEIDENTIFIER NULL;
GO

-- 2. Drop old CHECK constraint if exists and recreate with valid status values
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_FNB_ORDERS_STATUS')
BEGIN
    ALTER TABLE [FNB_ORDERS] DROP CONSTRAINT [CK_FNB_ORDERS_STATUS];
END
GO

ALTER TABLE [FNB_ORDERS] WITH NOCHECK
ADD CONSTRAINT [CK_FNB_ORDERS_STATUS]
CHECK ([order_status] IN ('PENDING', 'CONFIRMED', 'PAID', 'CANCELLED'));
GO

-- 3. Add index for faster lookups (optional but recommended)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FNB_ORDERS_booking_id')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_FNB_ORDERS_booking_id] ON [FNB_ORDERS] ([booking_id]);
END
GO

-- 4. Fix existing orders with invalid status (if any)
UPDATE [FNB_ORDERS] SET [order_status] = 'CONFIRMED' WHERE [order_status] = 'ACTIVE';
GO
