namespace EventService.WebApi.Middleware;

public class TenantMiddleware
{
    private const string TenantHeader = "X-Tenant-Id";
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TenantHeader, out var tenantId))
        {
            context.Items["TenantId"] = tenantId.ToString();
        }

        await _next(context);
    }
}
