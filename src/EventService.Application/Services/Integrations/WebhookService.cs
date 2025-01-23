using EventService.Application.Interfaces.Repositories;
using EventService.Application.Interfaces.Services.Caching;
using EventService.Application.Interfaces.Services.Integrations;
using EventService.Application.Models.Integrations;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Integrations;
using Microsoft.Extensions.Logging;
using Polly;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace EventService.Application.Services.Integrations
{
    public class WebhookService : IWebhookService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICacheService _cacheService;
        private readonly IWebhookRepository _webhookRepository;
        private readonly ILogger<WebhookService> _logger;

        public WebhookService(
            IHttpClientFactory httpClientFactory,
            ICacheService cacheService,
            IWebhookRepository webhookRepository,
            ILogger<WebhookService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cacheService = cacheService;
            _webhookRepository = webhookRepository;
            _logger = logger;
        }

        /// <summary>
        /// Triggers all webhooks for a given event.
        /// </summary>
        public async Task TriggerWebhooksAsync(Event eventEntity)
        {
            var cacheKey = $"webhooks:{eventEntity.BusinessId}";
            var webhooks = await _cacheService.GetAsync<List<Webhook>>(cacheKey);

            if (webhooks is null)
            {
                _logger.LogInformation("🔎 Loading webhooks from database for Business {BusinessId}", eventEntity.BusinessId);
                webhooks = await _webhookRepository.GetByBusinessIdAsync(eventEntity.BusinessId);

                if (webhooks is not null)
                {
                    await _cacheService.SetAsync(cacheKey, webhooks, TimeSpan.FromMinutes(30)); // Cache for 30 minutes
                }
                else
                {
                    _logger.LogWarning("⚠️ No webhooks found for Business {BusinessId}", eventEntity.BusinessId);
                    return;
                }
            }

            foreach (var webhook in webhooks)
            {
                bool success = await SendWebhookAsync(webhook.Id, new
                {
                    EventId = eventEntity.Id,
                    Title = eventEntity.Title,
                    Description = eventEntity.Description,
                    ScheduledAt = eventEntity.ScheduledAt
                });

                if (!success)
                {
                    _logger.LogWarning("❌ Webhook failed for URL {WebhookUrl}, adding to Dead Letter Queue", webhook.Url);
                    var retryPayload = new WebhookRetryPayload(webhook.Id, eventEntity);
                    await _cacheService.SetAsync($"dlq:webhooks:{webhook.Id}", retryPayload, TimeSpan.FromDays(1)); // DLQ for 24 hours
                }
            }
        }

        /// <summary>
        /// Sends a webhook request with retries.
        /// </summary>
        public async Task<bool> SendWebhookAsync(Guid webhookId, object data)
        {
            var webhook = await _webhookRepository.GetByIdAsync(webhookId);
            if (webhook is null) return false;

            var payload = new WebhookRetryPayload(webhook.Id, data);
            var payloadJson = JsonSerializer.Serialize(payload);

            var signature = GenerateHmacSignature(webhook.SecretKey, payloadJson); // ✅ Generate Signature

            var httpClient = _httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, webhook.Url)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("X-Signature", signature); // ✅ Add HMAC signature header
            request.Headers.Add("X-Webhook-Id", webhook.Id.ToString()); // ✅ Add Webhook ID

            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(5, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("🔄 Retrying webhook {WebhookId}, attempt {RetryCount}", webhook.Id, retryCount);
                    });

            try
            {
                var response = await retryPolicy.ExecuteAsync(() => httpClient.SendAsync(request));

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ Webhook sent successfully: {WebhookId}", webhook.Id);
                    return true;
                }

                _logger.LogWarning("❌ Webhook failed: {WebhookId}, Status Code: {StatusCode}", webhook.Id, response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Webhook request failed: {WebhookId}", webhook.Id);
            }

            var failureKey = $"failures:webhook:{webhook.Id}";
            var failureCount = await _cacheService.GetAsync<int>(failureKey);
            await _cacheService.SetAsync(failureKey, failureCount + 1, TimeSpan.FromHours(6));
            return false;
        }

        /// <summary>
        /// ✅ Generate HMAC SHA-256 Signature
        /// </summary>
        private string GenerateHmacSignature(string secretKey, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(hash);
        }
    }
}
