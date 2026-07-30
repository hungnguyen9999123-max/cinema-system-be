-- Migration: Create GUEST customer user for counter F&B orders
-- Date: 2026-07-27
-- Description: Counter orders need a default customer when no customer info is provided

-- Check if guest user already exists
IF NOT EXISTS (SELECT 1 FROM [USERS] WHERE [id] = '00000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO [USERS] ([id], [email], [password_hash], [full_name], [phone], [role], [status], [created_at], [updated_at])
    VALUES (
        '00000000-0000-0000-0000-000000000001',
        'guest@cinema.local',
        '$2a$11$dummyhashforguestuser00000000000000000000000000000',
        'Khách vãng lai',
        NULL,
        'Customer',
        'ACTIVE',
        GETUTCDATE(),
        GETUTCDATE()
    );
END
GO

PRINT 'Guest customer user created or already exists.';
