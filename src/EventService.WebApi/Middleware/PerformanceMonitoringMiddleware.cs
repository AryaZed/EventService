using Serilog;
using System.Diagnostics;

namespace EventService.WebApi.Middleware;

public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private const int SlowRequestThresholdMs = 500; // Define threshold for slow requests

    public PerformanceMonitoringMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        await _next(context);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
        {
            Log.Warning("SLOW REQUEST: {Method} {Path} took {ElapsedMilliseconds}ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}
