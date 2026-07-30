using System;
using System.Collections.Generic;
using System.Text;

namespace CinemaSystem.Common
{
    public sealed record PagedResult<T>(
     IReadOnlyList<T> Items,
     int Page,
     int PageSize,
     int TotalCount,
     int TotalPages);
}
