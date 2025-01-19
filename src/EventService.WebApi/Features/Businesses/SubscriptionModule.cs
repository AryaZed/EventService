using Carter;
using EventService.Application.Services.Businesses;

namespace EventService.WebApi.Features.Businesses;

public class SubscriptionModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/subscriptions").WithTags("Subscriptions");

        group.MapPut("/{businessId:guid}/change/{newPlanId:guid}", async (
            SubscriptionService subscriptionService,
            Guid businessId,
            Guid newPlanId) =>
        {
            var success = await subscriptionService.ChangeSubscriptionPlanAsync(businessId, newPlanId);
            return success ? Results.Ok("Subscription changed successfully.") : Results.BadRequest("Failed to change subscription.");
        });
    }
}
