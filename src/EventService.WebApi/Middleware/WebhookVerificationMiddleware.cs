using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace EventService.WebApi.Middleware
{
    public class WebhookVerificationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<WebhookVerificationMiddleware> _logger;
        private readonly ICacheService _cacheService;
        private readonly IWebhookRepository _webhookRepository;

        public WebhookVerificationMiddleware(
            RequestDelegate next,
            ILogger<WebhookVerificationMiddleware> logger,
            ICacheService cacheService,
            IWebhookRepository webhookRepository)
        {
            _next = next;
            _logger = logger;
            _cacheService = cacheService;
            _webhookRepository = webhookRepository;
        }

        public async Task Invoke(HttpContext context)
        {
            var webhookId = context.Request.Headers["X-Webhook-Id"].ToString();
            if (string.IsNullOrEmpty(webhookId))
            {
                _logger.LogWarning("❌ Missing Webhook ID in request headers");
                context.Response.StatusCode = 400; // Bad Request
                return;
            }

            var secretKey = await GetWebhookSecretKeyAsync(webhookId);
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogWarning("❌ Webhook ID {WebhookId} not found", webhookId);
                context.Response.StatusCode = 403; // Forbidden
                return;
            }

            var signatureHeader = context.Request.Headers["X-Signature"].ToString();
            if (string.IsNullOrEmpty(signatureHeader))
            {
                _logger.LogWarning("❌ Missing Webhook signature header");
                context.Response.StatusCode = 403;
                return;
            }

            var requestBody = await new StreamReader(context.Request.Body).ReadToEndAsync();
            var computedSignature = GenerateHmacSignature(requestBody, secretKey);

            if (signatureHeader != computedSignature)
            {
                _logger.LogWarning("❌ Webhook signature mismatch for Webhook ID {WebhookId}", webhookId);
                context.Response.StatusCode = 403;
                return;
            }

            await _next(context);
        }

        private string GenerateHmacSignature(string payload, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }

        private async Task<string?> GetWebhookSecretKeyAsync(string webhookId)
        {
            var cacheKey = $"webhook-secret:{webhookId}";

            // ✅ First, check in cache
            var secretKey = await _cacheService.GetAsync<string>(cacheKey);
            if (!string.IsNullOrEmpty(secretKey))
            {
                _logger.LogInformation("🔑 Webhook Secret Key retrieved from cache for {WebhookId}", webhookId);
                return secretKey;
            }

            // 🔎 Fallback: Fetch from database if not in cache
            var webhook = await _webhookRepository.GetByIdAsync(Guid.Parse(webhookId));
            if (webhook == null)
            {
                _logger.LogWarning("❌ Webhook ID {WebhookId} not found in database", webhookId);
                return null;
            }

            // ✅ Cache it for future use (1 hour expiry)
            await _cacheService.SetAsync(cacheKey, webhook.SecretKey, TimeSpan.FromHours(1));

            _logger.LogInformation("✅ Webhook Secret Key fetched from DB and cached for {WebhookId}", webhookId);
            return webhook.SecretKey;
        }
    }
}
