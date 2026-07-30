-- Align PAYMENTS status constraint to the canonical value used by code.
-- The application writes 'SUCCESS' for a paid payment. The constraint is tightened
-- to only allow the values that the application actually uses.

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id = OBJECT_ID('PAYMENTS')
      AND name = 'CK_PAY_STATUS'
)
    ALTER TABLE PAYMENTS DROP CONSTRAINT CK_PAY_STATUS;
GO

IF EXISTS (
    SELECT 1
    FROM PAYMENTS WITH (HOLDLOCK)
    WHERE status NOT IN ('PENDING', 'SUCCESS', 'FAILED', 'REFUNDED')
)
BEGIN
    RAISERROR('Cannot tighten CK_PAY_STATUS: data exists outside the allowed set.', 16, 1);
    RETURN;
END
GO

ALTER TABLE PAYMENTS
ADD CONSTRAINT CK_PAY_STATUS
CHECK (status IN ('PENDING', 'SUCCESS', 'FAILED', 'REFUNDED'));
GO
