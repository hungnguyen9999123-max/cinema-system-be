using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public sealed class FnbOrderSummaryDto
{
    public Guid OrderId { get; init; }
    public decimal TotalAmount { get; init; }
    public string OrderStatus { get; init; } = null!;
    public DateTime CreatedAt { get; init; }
    public List<FnbOrderItemSummaryDto> Items { get; init; } = new();
}

public sealed class FnbOrderItemSummaryDto
{
    public Guid ItemId { get; init; }
    public string ItemName { get; init; } = null!;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Subtotal { get; init; }
}
