using EventService.Domain.Entities.Businesses;

namespace EventService.Domain.Entities.Integrations;

public class Webhook
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BusinessId { get; private set; }
    public string Url { get; private set; }
    public string SecretKey { get; private set; }
    public string EventType { get; private set; } // "event.created", "event.updated"
    public Business Business { get; private set; }

    private Webhook() { }

    public Webhook(Guid businessId, string url, string secretKey, string eventType)
    {
        BusinessId = businessId;
        Url = url;
        SecretKey = secretKey;
        EventType = eventType;
    }
}
