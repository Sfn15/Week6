using System.Net;
using System.Text.Json;

namespace Week6.Middleware;

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
        catch (Exception e)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? "unknown";

            _logger.LogError(e,"Unhandled exception for {Method} {Path} - CorrelataionId: {CorrelationId}",
            context.Request.Method, context.Request.Path, correlationId);  

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int) HttpStatusCode.InternalServerError;

            var response = new
            {
                error = "AN unexpected error occurred.",
                correlationId,
                detail = context.RequestServices.GetRequiredService<IHostEnvironment>().IsDevelopment()
                ? e.Message : null
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }
}