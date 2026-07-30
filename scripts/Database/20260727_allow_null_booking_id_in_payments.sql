-- Migration: Allow NULL booking_id for F&B payments in PAYMENTS table
-- Date: 2026-07-27
-- Description: F&B payments (VNPay) don't have booking_id, need to allow NULL

ALTER TABLE [PAYMENTS] ALTER COLUMN [booking_id] UNIQUEIDENTIFIER NULL;
GO

PRINT 'PAYMENTS.booking_id now allows NULL for F&B payments.';
