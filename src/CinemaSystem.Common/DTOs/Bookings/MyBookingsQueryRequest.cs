using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public sealed class MyBookingsQueryRequest
{
    public string? Status { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed class MyBookingsPagedResultDto
{
    public IReadOnlyList<MyBookingListItemDto> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}