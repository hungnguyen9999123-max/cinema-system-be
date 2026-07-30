-- Migration: Add CONFIRMED status to FNB_ORDERS order_status CHECK constraint
-- Date: 2026-07-27
-- Description: VNPay flow needs CONFIRMED status for paid F&B orders

-- Drop existing CHECK constraint
DECLARE @constraintName NVARCHAR(128);
SELECT @constraintName = c.name
FROM sys.check_constraints c
JOIN sys.tables t ON c.parent_object_id = t.object_id
WHERE t.name = 'FNB_ORDERS' AND c.name LIKE 'CK_FNB_ORDERS%';

IF @constraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [FNB_ORDERS] DROP CONSTRAINT [' + @constraintName + ']');
END

-- Add new CHECK constraint with CONFIRMED
ALTER TABLE [FNB_ORDERS]
ADD CONSTRAINT CK_FNB_ORDERS_ORDER_STATUS
CHECK ([order_status] IN ('PENDING', 'CONFIRMED', 'PAID', 'CANCELLED'));

PRINT 'FNB_ORDERS CHECK constraint updated to include CONFIRMED status.';
