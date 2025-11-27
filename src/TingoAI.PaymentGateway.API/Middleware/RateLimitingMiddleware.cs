using TingoAI.PaymentGateway.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;

namespace TingoAI.PaymentGateway.API.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly RedisCache _cache;
    private readonly int _requestsPerMinute;
    private readonly bool _enabled;

    public RateLimitingMiddleware(RequestDelegate next, RedisCache cache, IConfiguration configuration)
    {
        _next = next;
        _cache = cache;
        _requestsPerMinute = configuration.GetValue<int>("RateLimit:RequestsPerMinute", 100);
        _enabled = configuration.GetValue<bool>("RateLimit:EnableRateLimiting", true);
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_enabled)
        {
            await _next(context);
            return;
        }

        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var key = $"ratelimit:{ipAddress}";

        var requestCount = await _cache.IncrementAsync(key, TimeSpan.FromMinutes(1));

        if (requestCount > _requestsPerMinute)
        {
            context.Response.StatusCode = 429; // Too Many Requests
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Rate limit exceeded",
                message = $"Maximum {_requestsPerMinute} requests per minute allowed",
                retryAfter = 60
            });
            return;
        }

        context.Response.Headers.Append("X-RateLimit-Limit", _requestsPerMinute.ToString());
        context.Response.Headers.Append("X-RateLimit-Remaining", (_requestsPerMinute - requestCount).ToString());

        await _next(context);
    }
}
