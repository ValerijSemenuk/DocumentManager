using System.Net;
using System.Text.Json;

namespace DocumentManager.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            // Busirule -> 400
            var message = ex.Message;
            if (message.Contains("unique", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("must be", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("not empty", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("depth", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("too large", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
                return;
            }

            // KeyNotFoundException -> 404
            if (ex is KeyNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
                return;
            }

            // Інше -> 500
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Internal server error" }));
        }
    }
}