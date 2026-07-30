using CinemaSystem.Common.DTOs.Pos;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Pos;

public interface IPosBookingService
{
    Task<PosCreateTicketResponse> CreatePosTicketAsync(
        Guid staffId,
        CreatePosBookingRequest request,
        CancellationToken cancellationToken = default);
}