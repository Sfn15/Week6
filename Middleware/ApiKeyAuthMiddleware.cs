namespace Week6.Middleware;

public class ApiKeyAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthMiddleware> _logger;
    private const string ApiKeyHeaderName = "X-Api-Key";

    private static readonly HashSet<string> _validKeys = new()
    {
      "api key (change later)"  
    };

    private static readonly string[] _openPaths = {"/swagger", "/health", "/openapi"};

    public ApiKeyAuthMiddleware(RequestDelegate next, ILogger<ApiKeyAuthMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        if(_openPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedKey) ||
        !_validKeys.Contains(providedKey.ToString()))
        {
            _logger.LogWarning("Rejected request with missing/invalid API key for {Path}", path);
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Missing or invalid API key.");
            return;
        }


        await _next(context);
    }
}