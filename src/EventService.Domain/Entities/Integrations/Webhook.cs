using EventService.Domain.Entities.Businesses;

namespace EventService.Domain.Entities.Integrations;

public class Webhook
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BusinessId { get; private set; }
    public string Url { get; private set; }
    public string SecretKey { get; private set; } // ✅ Used for HMAC verification
    public string EventType { get; private set; }
    public Business Business { get; private set; }

    private Webhook() { }

    public Webhook(Guid businessId, string url, string secretKey, string eventType)
    {
        BusinessId = businessId;
        Url = url;
        SecretKey = secretKey; // ✅ Generate per webhook
        EventType = eventType;
    }

    public void Update(string url, string eventType, string? secretKey = null)
    {
        Url = url;
        EventType = eventType;
        if (!string.IsNullOrEmpty(secretKey))
        {
            SecretKey = secretKey; // ✅ Allow secret rotation
        }
    }
}
