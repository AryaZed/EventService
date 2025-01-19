using EventService.Domain.Entities.Integrations;
using EventService.Domain.Entities.Users;

namespace EventService.Domain.Entities.Businesses;

public class Business
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Name { get; private set; }
    public string ContactEmail { get; private set; }
    public string PhoneNumber { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    public List<User> Users { get; private set; } = new();
    public List<Webhook> Webhooks { get; private set; } = new();

    public Guid SubscriptionPlanId { get; private set; }
    public SubscriptionPlan SubscriptionPlan { get; private set; }

    public DateTime SubscriptionStartDate { get; private set; }
    public DateTime SubscriptionEndDate { get; private set; }

    private Business() { }

    public Business(string name, string contactEmail, string phoneNumber, SubscriptionPlan subscriptionPlan)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ContactEmail = contactEmail ?? throw new ArgumentNullException(nameof(contactEmail));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));

        SubscriptionPlan = subscriptionPlan ?? throw new ArgumentNullException(nameof(subscriptionPlan));
        SubscriptionPlanId = subscriptionPlan.Id;

        SubscriptionStartDate = DateTime.UtcNow;
        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1); // Default to 1 month
    }

    // ✅ Factory method for creating a new business with a subscription
    public static Business Create(string name, string contactEmail, string phoneNumber, SubscriptionPlan subscriptionPlan)
    {
        return new Business(name, contactEmail, phoneNumber, subscriptionPlan);
    }

    // ✅ Factory method for retrieving an existing business
    public static Business CreateExisting(Guid id)
    {
        return new Business { Id = id };
    }

    // ✅ Update method to allow updating business details
    public void Update(string name, string contactEmail, string phoneNumber, SubscriptionPlan subscriptionPlan)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        ContactEmail = contactEmail ?? throw new ArgumentNullException(nameof(contactEmail));
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));

        ChangeSubscription(subscriptionPlan);
    }

    public void UpgradeSubscription(SubscriptionPlan newPlan)
    {
        ChangeSubscription(newPlan);
    }

    public void DowngradeSubscription(SubscriptionPlan newPlan)
    {
        ChangeSubscription(newPlan);
    }

    private void ChangeSubscription(SubscriptionPlan newPlan)
    {
        if (newPlan == null) throw new ArgumentNullException(nameof(newPlan));

        SubscriptionPlan = newPlan;
        SubscriptionPlanId = newPlan.Id;
        SubscriptionStartDate = DateTime.UtcNow;
        SubscriptionEndDate = DateTime.UtcNow.AddMonths(1); // Restart billing cycle
    }
}
