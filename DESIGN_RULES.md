# Cinema System Backend Design Rules

Tài liệu này ghi lại các quy tắc thiết kế đang được dùng trong backend của Cinema System.

## 1. Kiến trúc nhiều lớp

- `CinemaSystem.API` chỉ nhận request, trả response, gắn middleware, auth, swagger và DI.
- `CinemaSystem.Services` chứa business logic.
- `CinemaSystem.DAL` chứa DbContext, entity và repository.
- `CinemaSystem.Common` chứa DTO, enum, response wrapper và constants dùng chung.

## 2. Luồng xử lý chuẩn

1. Controller nhận request.
2. FluentValidation kiểm tra input.
3. Service xử lý nghiệp vụ.
4. Repository làm việc với database.
5. Controller trả `ApiResponse<T>`.

## 3. Quy tắc cho API

- Dùng REST route rõ ràng, ví dụ: `/api/auth`, `/api/movies`.
- Dùng HTTP status code đúng ngữ nghĩa:
  - `200 OK` cho thành công
  - `201 Created` cho tạo mới
  - `400 BadRequest` cho dữ liệu sai
  - `401 Unauthorized` cho token/phiên không hợp lệ
  - `404 NotFound` cho dữ liệu không tồn tại
  - `409 Conflict` cho xung đột nghiệp vụ
- Trả dữ liệu theo wrapper `ApiResponse<T>`, không trả raw object trực tiếp.

## 4. Quy tắc DTO

- Không dùng entity database trực tiếp cho request/response.
- Request DTO phải có validation rõ ràng.
- Response DTO chỉ chứa dữ liệu cần cho client.
- Tên DTO phải theo mục đích:
  - `Create...Request`
  - `Update...Request`
  - `...Response`
  - `...SearchRequest`

## 5. Quy tắc validation

- Dùng FluentValidation cho input phức tạp.
- Dùng Data Annotation cho ràng buộc đơn giản trên DTO.
- Email, password, confirm password, range, string length phải được kiểm tra trước khi vào service.

## 6. Quy tắc service

- Service là nơi đặt business logic chính.
- Service không được phụ thuộc vào controller.
- Service làm sạch dữ liệu trước khi lưu:
  - `Trim()`
  - chuẩn hóa giá trị optional về `null`
- Service trả dữ liệu qua DTO, không trả entity.

## 7. Quy tắc repository / DAL

- Repository chỉ lo truy vấn và thao tác dữ liệu.
- Dùng `DbContext` thông qua interface/repository, không query database trực tiếp trong controller.
- Kiểm tra quan hệ nghiệp vụ ở DAL khi cần, ví dụ movie không được xóa nếu còn showtime.

## 8. Quy tắc authentication và authorization

- Dùng JWT Bearer cho bảo mật API.
- Phân quyền bằng role:
  - `Admin`
  - `Manager`
  - `Staff`
  - `Customer`
- Các endpoint thay đổi dữ liệu phải gắn `[Authorize]`.
- Refresh token phải được hash trước khi lưu.
- Email verification là bước bắt buộc trước khi login.

## 9. Quy tắc lỗi và exception

- Dùng `GlobalExceptionMiddleware` để gom lỗi về một nơi.
- ValidationException trả về `400`.
- UnauthorizedAccessException trả về `401`.
- InvalidOperationException trả về `400`.
- Exception không xử lý phải được log và trả lỗi chuẩn.

## 10. Quy tắc cấu hình

- Cấu hình lấy từ `appsettings.json` và environment.
- Không hardcode secret trong code.
- Các giá trị nhạy cảm như JWT key, SMTP password, connection string phải được thay bằng biến môi trường hoặc secret manager khi deploy.

## 11. Quy tắc upload file

- File upload chỉ đi qua endpoint riêng.
- Chỉ lưu đường dẫn/URL vào database, không lưu file nhị phân trong entity.
- Nếu thao tác upload thất bại giữa chừng thì phải dọn file đã tạo để tránh rác.

## 12. Quy tắc đặt tên

- Controller: `XxxController`
- Service interface: `IXxxService`
- Service implementation: `XxxService`
- Repository interface: `IXxxRepository`
- Repository implementation: `XxxRepository`
- Entity và DTO dùng tên mô tả nghiệp vụ rõ ràng.

## 13. Mục tiêu thiết kế

- Tách biệt trách nhiệm rõ ràng.
- Dễ test và dễ bảo trì.
- Có thể mở rộng thêm module như booking, ticket, payment, promotion mà không phải đổi kiến trúc lớn.

