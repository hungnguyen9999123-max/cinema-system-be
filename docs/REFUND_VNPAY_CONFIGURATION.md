# Hoàn tiền vào ví khách hàng

VNPAY chỉ được dùng cho bước thanh toán. Khi Manager duyệt yêu cầu hoàn tiền, hệ thống **không gọi API refund của VNPAY**: khoản tiền được ghi vào ví nội bộ của khách hàng trong cùng một giao dịch dữ liệu với việc đóng booking, hủy vé và nhả ghế.

Luồng vận hành:

1. Khách hàng tạo yêu cầu hoàn tiền từ vé đủ điều kiện.
2. Manager duyệt yêu cầu. Booking chuyển sang `REFUNDED`, vé chuyển `CANCELLED`, ghế được `RELEASED`, và ví nhận một giao dịch `REFUND_CREDIT`.
3. Khách tạo yêu cầu rút tiền, cung cấp thông tin nhận tiền. Số tiền được giữ lại khỏi số dư khả dụng bằng giao dịch `WITHDRAWAL_HOLD`.
4. Manager thực hiện chuyển khoản ngoài hệ thống rồi chọn **Ghi nhận đã chuyển** và nhập mã đối chiếu. Hệ thống lưu trạng thái `COMPLETED`.
5. Nếu chưa chuyển khoản, Manager có thể từ chối yêu cầu rút; hệ thống hoàn lại tiền vào ví bằng `WITHDRAWAL_REVERSAL`.

Các endpoint chính:

- Khách: `GET /api/wallet`, `POST /api/wallet/withdrawals`, `GET /api/wallet/withdrawals/me`.
- Manager/Admin: `GET /api/ops/withdrawals`, `POST /api/ops/withdrawals/{id}/complete`, `POST /api/ops/withdrawals/{id}/reject`.

Áp dụng hai script vào database trước khi chạy:

1. `scripts/Database/20260722_wallet_refund_schema.sql`
2. `scripts/Database/20260722_wallet_refund_ticket_status_patch.sql`

API có idempotency cho yêu cầu rút tiền và kiểm tra chống ghi cóp hoàn tiền theo refund. Thông tin tài khoản nhận tiền chỉ được dùng cho bước chuyển khoản thủ công; không lưu bí mật thanh toán của VNPAY trong tài liệu hoặc cấu hình được commit.
