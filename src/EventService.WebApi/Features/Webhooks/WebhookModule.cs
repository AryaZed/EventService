using Carter;
using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Integrations;

namespace EventService.WebApi.Features.Webhooks
{
    public class WebhookModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/webhooks").WithTags("Webhooks");

            group.MapGet("/{businessId:guid}", async (IWebhookRepository repo, Guid businessId) =>
            {
                var webhooks = await repo.GetByBusinessIdAsync(businessId);
                return Results.Ok(webhooks.Select(WebhookResponse.FromEntity));
            });

            group.MapGet("/{id:guid}", async (IWebhookRepository repo, Guid id) =>
            {
                var webhook = await repo.GetByIdAsync(id);
                return webhook is not null ? Results.Ok(WebhookResponse.FromEntity(webhook)) : Results.NotFound();
            });

            group.MapPost("/", async (IWebhookRepository repo, WebhookRequest request, HttpContext context) =>
            {
                if (!context.Items.TryGetValue("TenantId", out var tenantId) || tenantId is not string businessIdStr)
                {
                    return Results.BadRequest("Missing or invalid Business ID");
                }

                var businessId = Guid.Parse(businessIdStr);
                var webhook = new Webhook(businessId, request.Url, request.SecretKey, request.EventType);
                await repo.AddAsync(webhook);

                return Results.Created($"/api/webhooks/{webhook.Id}", WebhookResponse.FromEntity(webhook));
            });

            group.MapPut("/{id:guid}", async (IWebhookRepository repo, Guid id, WebhookRequest request) =>
            {
                var webhook = await repo.GetByIdAsync(id);
                if (webhook is null) return Results.NotFound();

                webhook.Update(request.Url, request.EventType, request.SecretKey);
                await repo.UpdateAsync(webhook);

                return Results.NoContent();
            });

            group.MapDelete("/{id:guid}", async (IWebhookRepository repo, Guid id) =>
            {
                await repo.DeleteAsync(id);
                return Results.NoContent();
            });

            group.MapPut("/{id:guid}/rotate-secret", async (IWebhookRepository repo, Guid id) =>
            {
                var webhook = await repo.GetByIdAsync(id);
                if (webhook is null) return Results.NotFound();

                var newSecretKey = Guid.NewGuid().ToString(); // ✅ Generate new secure key
                webhook.Update(webhook.Url, webhook.EventType, newSecretKey);

                await repo.UpdateAsync(webhook);
                return Results.Ok(new { NewSecretKey = newSecretKey });
            });

        }
    }
}
