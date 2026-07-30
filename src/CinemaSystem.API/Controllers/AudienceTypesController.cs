using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.DAL.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/audience-types")]
public sealed class AudienceTypesController(IAudienceTypeRepository audienceTypeRepository) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<IEnumerable<AudienceTypeDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<AudienceTypeDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        var types = await audienceTypeRepository.GetAllActiveAsync(cancellationToken);

        var dtos = types.Select(a => new AudienceTypeDto
        {
            Id = a.Id,
            Code = a.Code,
            DisplayName = a.DisplayName,
            AudienceMultiplier = a.AudienceMultiplier,
            Description = a.Description
        });

        return Ok(ApiResponse<IEnumerable<AudienceTypeDto>>.Success(dtos, "Audience types retrieved successfully."));
    }
}
