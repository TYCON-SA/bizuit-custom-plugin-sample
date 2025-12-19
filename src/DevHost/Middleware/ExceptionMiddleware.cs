using System.Diagnostics;
using System.Text.Json;

namespace DevHost.Middleware;

/// <summary>
/// Exception middleware that captures unhandled exceptions and returns detailed error information in Development mode.
/// In Production mode, returns generic error messages to avoid leaking sensitive information.
/// </summary>
public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _env;

    public ExceptionMiddleware(
        RequestDelegate next,
        ILogger<ExceptionMiddleware> logger,
        IWebHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Log exception with stack trace
        _logger.LogError(exception, "[DevHost] Unhandled exception: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var response = _env.IsDevelopment()
            ? CreateDevelopmentErrorResponse(exception)
            : CreateProductionErrorResponse();

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        await context.Response.WriteAsync(json);
    }

    private object CreateDevelopmentErrorResponse(Exception exception)
    {
        var innerExceptions = new List<object>();
        var inner = exception.InnerException;
        while (inner != null)
        {
            innerExceptions.Add(new
            {
                type = inner.GetType().FullName,
                message = inner.Message,
                stackTrace = inner.StackTrace?.Split('\n').Select(line => line.Trim()).ToArray()
            });
            inner = inner.InnerException;
        }

        return new
        {
            error = "Internal Server Error",
            message = exception.Message,
            type = exception.GetType().FullName,
            stackTrace = exception.StackTrace?.Split('\n').Select(line => line.Trim()).ToArray(),
            innerExceptions = innerExceptions.Any() ? innerExceptions : null,
            source = exception.Source,
            targetSite = exception.TargetSite?.Name,
            timestamp = DateTime.UtcNow,
            environment = "Development",
            helpText = "This detailed error is only shown in Development mode. In Production, generic errors are returned."
        };
    }

    private object CreateProductionErrorResponse()
    {
        return new
        {
            error = "Internal Server Error",
            message = "An unexpected error occurred. Please contact support if the issue persists.",
            timestamp = DateTime.UtcNow,
            environment = "Production"
        };
    }
}
