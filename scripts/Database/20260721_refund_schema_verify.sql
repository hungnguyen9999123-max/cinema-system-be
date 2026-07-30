SELECT name, TYPE_NAME(user_type_id) AS data_type, is_nullable
FROM sys.columns
WHERE object_id IN (OBJECT_ID(N'dbo.PAYMENTS'), OBJECT_ID(N'dbo.REFUNDS'))
  AND name IN (N'gateway_request_at', N'requested_by', N'idempotency_key_hash', N'reason_code', N'row_version', N'next_reconciliation_at')
ORDER BY name;

SELECT name FROM sys.indexes
WHERE object_id IN (OBJECT_ID(N'dbo.REFUNDS'), OBJECT_ID(N'dbo.REFUND_GATEWAY_ATTEMPTS'))
  AND name IN (N'UX_REF_REQUESTER_IDEMPOTENCY', N'UX_REF_ACTIVE_PAYMENT', N'UQ_REF_ATTEMPT_NO', N'UQ_REF_ATTEMPT_REQUEST_ID')
ORDER BY name;

SELECT id, booking_id, amount, gateway, status
FROM dbo.PAYMENTS
WHERE status = N'SUCCESS' AND gateway_request_at IS NULL;
