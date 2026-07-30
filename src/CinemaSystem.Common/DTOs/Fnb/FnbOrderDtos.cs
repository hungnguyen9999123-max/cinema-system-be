using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Common.DTOs.Fnb;

public sealed record FnbOrderResponse(
    Guid Id,
    Guid? BookingId,
    Guid CustomerId,
    Guid? StaffId,
    decimal TotalAmount,
    string OrderStatus,
    DateTime CreatedAt,
    string? PaymentMethod,
    List<FnbOrderDetailResponse> Items);

public sealed record FnbOrderDetailResponse(
    Guid Id,
    Guid ItemId,
    string ItemName,
    int Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record CreateFnbOrderRequest
{
    [Required]
    public Guid BookingId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
    public List<CreateFnbOrderItemRequest> Items { get; init; } = new();
}

public sealed record CreateFnbCounterOrderRequest
{
    public Guid? CustomerId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
    public List<CreateFnbOrderItemRequest> Items { get; init; } = new();

    [Required]
    [AllowedValues("CASH", "CARD", "TRANSFER", "VNPAY", ErrorMessage = "Phương thức thanh toán không hợp lệ.")]
    public string PaymentMethod { get; init; } = "CASH";
}

public sealed class CreateFnbOrderItemRequest
{
    [Required]
    public Guid ItemId { get; init; }

    [Range(1, 100, ErrorMessage = "Số lượng phải từ 1 đến 100.")]
    public int Quantity { get; init; }
}

public sealed class UpdateFnbOrderStatusRequest
{
    [Required]
    [AllowedValues("PENDING", "CONFIRMED", "PREPARING", "READY", "COMPLETED", "SERVED", "CANCELLED", ErrorMessage = "Trạng thái không hợp lệ.")]
    public string Status { get; init; } = string.Empty;
}

public sealed record FnbOrderSearchRequest
{
    public Guid? BookingId { get; init; }

    public Guid? CustomerId { get; init; }

    public Guid? StaffId { get; init; }

    public string? Status { get; init; }

    public bool? IsCounterOrder { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = "Số trang phải lớn hơn 0.")]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = "Kích thước trang phải từ 1 đến 100.")]
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request for Staff/Manager/Admin to create F&B order linked to an existing booking.
/// Does NOT require the user to own the booking.
/// </summary>
public sealed record CreateFnbOrderForCounterRequest
{
    [Required]
    public Guid BookingId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = "Phải có ít nhất 1 sản phẩm.")]
    public List<CreateFnbOrderItemRequest> Items { get; init; } = new();

    public string? PaymentMethod { get; init; }
}
