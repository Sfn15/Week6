using System.Diagnostics;

namespace Week6.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var correlationId = Guid.NewGuid().ToString();
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        _logger.LogInformation(
            "Request started: {Method} {Path} - UserAgent: {UserAgent} - CorrelationId: {CorrelationId}",
            context.Request.Method, context.Request.Path, context.Request.Headers.UserAgent.ToString(), correlationId);

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "Request completed: {Method} {Path} - {StatusCode} - {ElapsedMs}ms - CorrelationId: {CorrelationId}",
                context.Request.Method, context.Request.Path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}