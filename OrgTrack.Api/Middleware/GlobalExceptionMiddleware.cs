using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;

namespace OrgTrack.Api.Middleware;

/// <summary>
/// A true ASP.NET middleware that intercepts ALL unhandled exceptions in the pipeline.
/// Without this, ASP.NET returns an error HTML or a JSON with a full stack trace — dangerous in production!
/// With this, any unexpected exception returns a clean JSON with a generic message.
/// </summary>
public class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excepție neașteptată pe {Method} {Path}", 
                context.Request.Method, context.Request.Path);

            await WriteErrorResponseAsync(context, ex);
        }
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static async Task WriteErrorResponseAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            ArgumentException         => HttpStatusCode.BadRequest,        // 400
            UnauthorizedAccessException => HttpStatusCode.Unauthorized,    // 401
            InvalidOperationException => HttpStatusCode.UnprocessableEntity, // 422
            KeyNotFoundException      => HttpStatusCode.NotFound,          // 404
            _                         => HttpStatusCode.InternalServerError // 500
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = statusCode == HttpStatusCode.InternalServerError
                ? "A apărut o eroare internă. Vă rugăm să încercați din nou."
                : ex.Message, // For business errors (400, 422) we display the actual message
            statusCode = (int)statusCode,
            timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(response, _jsonOptions);

        await context.Response.WriteAsync(json);
    }
}
