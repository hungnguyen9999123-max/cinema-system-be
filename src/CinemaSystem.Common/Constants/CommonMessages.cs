namespace CinemaSystem.Common.Constants;

public static class CommonMessages
{
    // System IDs
    public static readonly Guid GuestCustomerId = new("00000000-0000-0000-0000-000000000001");

    // Thành công
    public const string Retrieved = "Lấy dữ liệu thành công.";
    public const string Created = "Tạo mới thành công.";
    public const string Updated = "Cập nhật thành công.";
    public const string Deleted = "Xóa thành công.";
    public const string Uploaded = "Tải lên thành công.";

    // Lỗi chung
    public const string NotFound = "Không tìm thấy dữ liệu.";
    public const string AlreadyExists = "Dữ liệu đã tồn tại.";
    public const string CannotDelete = "Không thể xóa do còn dữ liệu liên quan.";
    public const string InvalidData = "Dữ liệu không hợp lệ.";

    // Validation
    public const string Required = "Trường bắt buộc không được để trống.";
    public const string InvalidFormat = "Định dạng không hợp lệ.";
    public const string InvalidEmail = "Email không hợp lệ.";
    public const string InvalidUrl = "URL không hợp lệ.";
    public const string OutOfRange = "Giá trị nằm ngoài phạm vi cho phép.";
    public const string MaxLengthExceeded = "Vượt quá độ dài cho phép.";
    public const string InvalidValue = "Giá trị không hợp lệ.";
    public const string EndDateAfterStartDate = "Ngày kết thúc phải sau hoặc bằng ngày bắt đầu.";

    // Tệp tin
    public const string FileEmpty = "Tệp không được để trống.";
    public const string FileTooLarge = "Kích thước tệp vượt quá giới hạn cho phép.";
    public const string FileInvalidType = "Loại tệp không được hỗ trợ.";

    // Xác thực
    public const string LoginSuccess = "Đăng nhập thành công.";
    public const string GoogleLoginSuccess = "Đăng nhập Google thành công.";
    public const string RegisterSuccess = "Đăng ký thành công. Vui lòng xác thực email trước khi đăng nhập.";
    public const string TokenRefreshed = "Làm mới token thành công.";
    public const string Verified = "Xác thực thành công.";
    public const string VerifiedResult = "Đã xác thực email.";
    public const string InvalidCredentials = "Email hoặc mật khẩu không đúng.";
    public const string InactiveAccount = "Tài khoản không hoạt động.";
    public const string EmailNotVerified = "Email chưa được xác thực.";
    public const string EmailAlreadyVerified = "Email đã được xác thực.";
    public const string InvalidToken = "Token không hợp lệ.";
    public const string InvalidGoogleToken = "Google ID token không hợp lệ.";
    public const string GoogleAccountAlreadyLinked = "Tài khoản Google này đã được liên kết với một email khác.";
    public const string EmailAlreadyLinkedToAnotherGoogleAccount = "Email này đã được liên kết với một tài khoản Google khác.";
    public const string ExpiredToken = "Token đã hết hạn.";
    public const string TokenReuseDetected = "Token không hợp lệ. Vui lòng đăng nhập lại.";
    public const string AccountNotAllowed = "Tài khoản không được phép thực hiện thao tác này.";
    public const string AccountLocked = "Tài khoản đã bị khóa tạm thời do đăng nhập sai nhiều lần. Vui lòng thử lại sau.";
    public const string PasswordTooShort = "Mật khẩu phải có ít nhất 8 ký tự.";
    public const string PasswordMismatch = "Mật khẩu xác nhận không khớp.";
}
