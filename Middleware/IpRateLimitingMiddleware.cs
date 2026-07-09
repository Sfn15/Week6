using System.Collections.Concurrent;

namespace Week6.Middleware;

public class IpRateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<IpRateLimitingMiddleware> _logger;

    private static readonly ConcurrentDictionary<String, (DateTime WindowStart, int Count)> _requests = new();

    private const int MaxRequestsPerWindow = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    private static readonly Dictionary<string, int> _endpointLimits = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/api/auth/login"] = 5,
        ["/api/orders"] = 30
    };

    public IpRateLimitingMiddleware(RequestDelegate next, ILogger<IpRateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "";
        var limit = _endpointLimits.TryGetValue(path, out var specificLimit) ? specificLimit : MaxRequestsPerWindow;

        var key = $"{ip}:{path}";
        var now = DateTime.UtcNow;

        var entry = _requests.AddOrUpdate(key,
        _ => (now,1), (_, existing) =>
        {
           if (now - existing.WindowStart > Window)
                return (now,1);
            return (existing.WindowStart, existing.Count +1); 
        });


        if (entry.Count > limit)
        {
            _logger.LogWarning("Rate limit exceeded for {Ip} on {Path}", ip, path);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = Window.TotalSeconds.ToString();
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        await _next(context);
    }
}