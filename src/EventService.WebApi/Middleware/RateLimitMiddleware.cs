using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.RateLimiting;

namespace EventService.WebApi.Middleware
{
    public class RateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IBusinessRepository _businessRepository;
        private readonly IRateLimitStore _rateLimitStore;
        private readonly IConfiguration _configuration;

        public RateLimitMiddleware(RequestDelegate next, IBusinessRepository businessRepository, IRateLimitStore rateLimitStore, IConfiguration configuration)
        {
            _next = next;
            _businessRepository = businessRepository;
            _rateLimitStore = rateLimitStore;
            _configuration = configuration;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Items["TenantId"] is not string tenantIdString || !Guid.TryParse(tenantIdString, out var tenantId))
            {
                await HandleRateLimitExceeded(context, "Tenant ID is missing or invalid.");
                return;
            }

            var business = await _businessRepository.GetBusinessByTenantAsync(tenantId);
            if (business == null)
            {
                await HandleRateLimitExceeded(context, "Business not found.");
                return;
            }

            var subscriptionPlan = business.SubscriptionPlan?.Name ?? "Free";
            var rateLimits = _configuration.GetSection($"SubscriptionPlans:{subscriptionPlan}").Get<RateLimitConfig>();

            if (rateLimits == null || !await _rateLimitStore.IsAllowedAsync(tenantId, rateLimits.MaxRequestsPerMinute, rateLimits.MaxRequestsPerHour))
            {
                await HandleRateLimitExceeded(context, "Rate limit exceeded. Upgrade your subscription.");
                return;
            }

            await _next(context);
        }

        private async Task HandleRateLimitExceeded(HttpContext context, string message)
        {
            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message }));
        }
    }

    public class RateLimitConfig
    {
        public int MaxRequestsPerMinute { get; set; }
        public int MaxRequestsPerHour { get; set; }
    }
}
