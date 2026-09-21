using ConferenceRoomBooking.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Middleware;

// Catches exceptions from the whole pipeline and turns them into consistent JSON error responses
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
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
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Invalid request"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        var isUnexpected = statusCode == StatusCodes.Status500InternalServerError;

        if (isUnexpected)
            _logger.LogError(exception, "Unhandled exception");
        else
            _logger.LogWarning("Request failed: {Message}", exception.Message);

        // Never expose internal details of unexpected errors to the client
        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = isUnexpected ? "An unexpected error occurred. Please try again later." : exception.Message,
            Instance = context.Request.Path
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}