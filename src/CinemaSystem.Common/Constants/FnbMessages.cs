namespace CinemaSystem.Common.Constants;

public static class FnbMessages
{
    public const string RetrievedSuccess = "Lấy danh sách F&B thành công.";
    public const string DetailRetrievedSuccess = "Lấy thông tin F&B thành công.";
    public const string CreatedSuccess = "Tạo F&B thành công.";
    public const string UpdatedSuccess = "Cập nhật F&B thành công.";
    public const string DeletedSuccess = "Đã chuyển F&B sang trạng thái không hoạt động.";
    public const string NotFound = "Không tìm thấy F&B.";
    public const string NameAlreadyExists = "Tên F&B đã tồn tại.";
    public const string InvalidType = "Loại F&B không hợp lệ.";
    public const string InvalidPrice = "Giá F&B phải lớn hơn 0.";

    public const string InvalidStatus = "Trạng thái F&B không hợp lệ.";
    public const string InvalidImageUrl = "Đường dẫn hình ảnh không hợp lệ.";
    public const string Required = "Trường bắt buộc không được để trống.";
    public const string MaxLengthExceeded = "Vượt quá độ dài cho phép.";
    public const string OutOfRange = "Giá trị nằm ngoài phạm vi cho phép.";
}

public static class FnbOrderMessages
{
    public const string RetrievedSuccess = "Lấy danh sách đơn hàng F&B thành công.";
    public const string DetailRetrievedSuccess = "Lấy thông tin đơn hàng F&B thành công.";
    public const string CreatedSuccess = "Tạo đơn hàng F&B thành công.";
    public const string CounterOrderCreatedSuccess = "Tạo đơn hàng F&B tại quầy thành công.";
    public const string UpdatedSuccess = "Cập nhật trạng thái đơn hàng F&B thành công.";
    public const string NotFound = "Không tìm thấy đơn hàng F&B.";
    public const string BookingNotFound = "Không tìm thấy booking.";
    public const string UnauthorizedBooking = "Bạn không có quyền đặt F&B cho booking này.";
    public const string BookingNotEligible = "Chỉ có thể đặt F&B khi booking đang chờ thanh toán hoặc đã xác nhận.";
    public const string ItemNotFound = "Không tìm thấy sản phẩm F&B: {0}";
    public const string ItemsNotFound = "Không tìm thấy một số sản phẩm F&B: {0}";
    public const string InvalidStatus = "Trạng thái đơn hàng không hợp lệ.";
    public const string InvalidStatusTransition = "Không thể chuyển từ trạng thái '{0}' sang '{1}'.";
    public const string InvalidQuantity = "Số lượng phải từ 1 đến 100.";
    public const string ItemInactive = "Sản phẩm '{0}' hiện không hoạt động.";
    public const string InvalidPaymentMethod = "Phương thức thanh toán không hợp lệ.";
}
