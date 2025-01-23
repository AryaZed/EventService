namespace EventService.WebApi.Features.Webhooks
{
    public record WebhookRequest(
       string Url,
       string EventType,
       string SecretKey
   );
}
