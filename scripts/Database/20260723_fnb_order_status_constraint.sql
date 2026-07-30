SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

BEGIN TRANSACTION;
GO

-- The application has always supported the full order lifecycle below.  Older
-- demo databases constrained FNB_ORDERS to a smaller set, which prevented a
-- paid booking from confirming its linked F&B order atomically.
IF OBJECT_ID(N'dbo.CK_FNB_ORDERS_STATUS', N'C') IS NOT NULL
BEGIN
    ALTER TABLE dbo.FNB_ORDERS DROP CONSTRAINT CK_FNB_ORDERS_STATUS;
END
GO

-- PAID was used by the first demo schema.  CONFIRMED is the equivalent
-- application state and is part of the supported F&B lifecycle.
UPDATE dbo.FNB_ORDERS
SET order_status = 'CONFIRMED'
WHERE order_status = 'PAID';
GO

ALTER TABLE dbo.FNB_ORDERS WITH CHECK
ADD CONSTRAINT CK_FNB_ORDERS_STATUS CHECK
(
    order_status IN
    (
        'PENDING',
        'CONFIRMED',
        'PREPARING',
        'READY',
        'COMPLETED',
        'SERVED',
        'CANCELLED'
    )
);
GO

COMMIT TRANSACTION;
GO
