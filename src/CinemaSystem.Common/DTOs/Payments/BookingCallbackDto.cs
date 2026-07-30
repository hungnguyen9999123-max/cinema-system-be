namespace CinemaSystem.Common.DTOs.Payments;

/// <summary>
/// Trả về booking + tickets cho staff hiển thị QR sau khi VNPay thanh toán thành công.
/// </summary>
public class BookingCallbackDto
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = string.Empty;
    public string MovieTitle { get; init; } = string.Empty;
    public string CinemaName { get; init; } = string.Empty;
    public string RoomName { get; init; } = string.Empty;
    public DateTime ShowtimeStart { get; init; }
    public DateTime ShowtimeEnd { get; init; }
    public List<string> SeatLabels { get; init; } = new();
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public decimal FnbTotalAmount { get; init; }
    public List<TicketCallbackDto> Tickets { get; init; } = new();
    public List<FnbOrderCallbackDto> FnbOrders { get; init; } = new();
}

public class TicketCallbackDto
{
    public Guid TicketId { get; init; }
    public string SeatLabel { get; init; } = string.Empty;
    public string QrImageBase64 { get; init; } = string.Empty;
    public string Token { get; init; } = string.Empty;
}

public class FnbOrderCallbackDto
{
    public Guid Id { get; init; }
    public decimal TotalAmount { get; init; }
    public string OrderStatus { get; init; } = string.Empty;
    public List<FnbOrderItemCallbackDto> Items { get; init; } = new();
}

public class FnbOrderItemCallbackDto
{
    public Guid Id { get; init; }
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
