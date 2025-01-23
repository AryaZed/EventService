namespace EventService.WebApi.Features.Webhooks
{
    public record WebhookResponse(
         Guid Id,
         string Url,
         string EventType,
         Guid BusinessId
     )
    {
        public static WebhookResponse FromEntity(EventService.Domain.Entities.Integrations.Webhook webhook)
        {
            return new WebhookResponse(webhook.Id, webhook.Url, webhook.EventType, webhook.BusinessId);
        }
    }
}
