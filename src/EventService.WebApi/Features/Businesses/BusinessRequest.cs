using EventService.Domain.Entities.Businesses;

namespace EventService.WebApi.Features.Businesses;

public class BusinessRequest
{
    public required string Name { get; set; }
    public required string ContactEmail { get; set; }
    public required string PhoneNumber { get; set; }
    public required SubscriptionPlan SubscriptionPlan{ get; set; }
}