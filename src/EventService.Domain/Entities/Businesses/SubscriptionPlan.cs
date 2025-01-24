using System;
using EventService.Domain.Entities.Businesses;

namespace EventService.Domain.Entities.Businesses
{
    public class SubscriptionPlan
    {
        public Guid Id { get; private set; } = Guid.CreateVersion7(); // ✅ Use standard GUID instead of Version7 (EF Core support)
        public string Name { get; private set; }
        public int MaxEventsPerMonth { get; private set; }
        public decimal Price { get; private set; }

        // ✅ Rate Limiting Fields
        public int MaxRequestsPerMinute { get; private set; }
        public int MaxRequestsPerHour { get; private set; }

        // ✅ Allow Global & Custom Plans
        public Guid? BusinessId { get; private set; } // Nullable -> Global Plan (null) or Business-Specific Plan
        public Business? Business { get; private set; }

        private SubscriptionPlan() { } // Required for EF Core

        public SubscriptionPlan(string name, int maxEvents, decimal price, int maxRequestsPerMinute, int maxRequestsPerHour, Business? business = null)
        {
            Name = name;
            MaxEventsPerMonth = maxEvents;
            Price = price;
            MaxRequestsPerMinute = maxRequestsPerMinute;
            MaxRequestsPerHour = maxRequestsPerHour;
            Business = business;
            BusinessId = business?.Id; // If business is null, it's a global plan
        }

        // ✅ Factory Methods for Creating Subscription Plans
        public static SubscriptionPlan CreateGlobal(string name, int maxEvents, decimal price, int maxRequestsPerMinute, int maxRequestsPerHour)
        {
            return new SubscriptionPlan(name, maxEvents, price, maxRequestsPerMinute, maxRequestsPerHour, null);
        }

        public static SubscriptionPlan CreateForBusiness(string name, int maxEvents, decimal price, int maxRequestsPerMinute, int maxRequestsPerHour, Business business)
        {
            return new SubscriptionPlan(name, maxEvents, price, maxRequestsPerMinute, maxRequestsPerHour, business);
        }
    }
}
