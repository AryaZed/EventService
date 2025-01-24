using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
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
        private readonly IServiceScopeFactory _scopeFactory; // ✅ Use Scope Factory
        private readonly IConfiguration _configuration;
        private readonly ILogger<RateLimitMiddleware> _logger;

        public RateLimitMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<RateLimitMiddleware> logger)
        {
            _next = next;
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Items["TenantId"] is not string tenantIdString || !Guid.TryParse(tenantIdString, out var tenantId))
            {
                await HandleRateLimitExceeded(context, "Tenant ID is missing or invalid.");
                return;
            }

            using var scope = _scopeFactory.CreateScope(); // ✅ Fix: Use Scoped Dependency
            var businessRepository = scope.ServiceProvider.GetRequiredService<IBusinessRepository>();
            var rateLimitStore = scope.ServiceProvider.GetRequiredService<IRateLimitStore>();

            var business = await businessRepository.GetBusinessByTenantAsync(tenantId);
            if (business == null)
            {
                await HandleRateLimitExceeded(context, "Business not found.");
                return;
            }

            var subscriptionPlan = business.SubscriptionPlan?.Name ?? "Free";
            var rateLimits = _configuration.GetSection($"SubscriptionPlans:{subscriptionPlan}").Get<RateLimitConfig>();

            if (rateLimits == null)
            {
                _logger.LogWarning("🚨 Rate limit configuration missing for plan: {Plan}", subscriptionPlan);
                await HandleRateLimitExceeded(context, "Rate limit configuration missing.");
                return;
            }

            if (!await rateLimitStore.IsAllowedAsync(tenantId, rateLimits.MaxRequestsPerMinute, rateLimits.MaxRequestsPerHour))
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
