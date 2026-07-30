
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Exceptions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Text.Json;

namespace CinemaSystem.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, ApiResponse<object?>.Fail(ex.Errors.Select(error => error.ErrorMessage).ToList(), "Validation failed"));
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.Unauthorized, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.BadRequest, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.NotFound, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, "Concurrency conflict detected.");
            await WriteResponseAsync(context, HttpStatusCode.Conflict, ApiResponse<object?>.Fail("The resource was modified by another request. Please retry."));
        }
        catch (BusinessConflictException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.Conflict, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (TooManyRequestsException ex)
        {
            context.Response.Headers.RetryAfter = ex.RetryAfterSeconds.ToString();
            await WriteResponseAsync(context, HttpStatusCode.TooManyRequests, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (ForbiddenAccessException ex)
        {
            await WriteResponseAsync(context, HttpStatusCode.Forbidden, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (CloudinaryOperationException ex)
        {
            _logger.LogError(ex, "Cloudinary operation failed.");
            await WriteResponseAsync(context, HttpStatusCode.BadGateway, ApiResponse<object?>.Fail(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception.");
            await WriteResponseAsync(
                context,
                HttpStatusCode.InternalServerError,
                ApiResponse<object?>.Fail("An unexpected error occurred."));
        }
    }

    private static async Task WriteResponseAsync<T>(HttpContext context, HttpStatusCode statusCode, ApiResponse<T> response)
    {
        context.Response.Clear();
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await context.Response.WriteAsync(json);
    }
}
