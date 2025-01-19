namespace EventService.Domain.Entities.Businesses;

public class SubscriptionPlan
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public int MaxEventsPerMonth { get; private set; }
    public decimal Price { get; private set; }

    public Guid? BusinessId { get; private set; } // ✅ Allow Global & Custom Plans
    public Business? Business { get; private set; }

    private SubscriptionPlan() { }

    public SubscriptionPlan(string name, int maxEvents, decimal price, Business? business = null)
    {
        Name = name;
        MaxEventsPerMonth = maxEvents;
        Price = price;
        Business = business;
        BusinessId = business?.Id; // Can be null if it's a global plan
    }
}
