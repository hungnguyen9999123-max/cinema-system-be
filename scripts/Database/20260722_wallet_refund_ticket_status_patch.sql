/*
  Wallet refund settlement cancels the issued ticket atomically.
  Older databases accepted only VALID / USED / VOID / EXPIRED and therefore
  rejected a successful refund transaction at commit time.
*/
IF OBJECT_ID(N'dbo.TICKETS', N'U') IS NOT NULL
BEGIN
    IF EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = N'CK_TICKETS_STATUS'
          AND parent_object_id = OBJECT_ID(N'dbo.TICKETS')
    )
    BEGIN
        ALTER TABLE dbo.TICKETS DROP CONSTRAINT CK_TICKETS_STATUS;
    END;

    ALTER TABLE dbo.TICKETS WITH CHECK
    ADD CONSTRAINT CK_TICKETS_STATUS
    CHECK ([status] IN (N'VALID', N'USED', N'VOID', N'EXPIRED', N'CANCELLED'));

    ALTER TABLE dbo.TICKETS CHECK CONSTRAINT CK_TICKETS_STATUS;
END;
