using Carter;
using EventService.Application.Interfaces.Repositories;

namespace EventService.WebApi.Features.Analytics
{
    public class EventAnalyticsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/event-analytics").WithTags("Event Analytics");

            group.MapGet("/", async (IEventAnalyticsRepository repo) =>
            {
                var analytics = await repo.GetAllAsync();
                return Results.Ok(analytics);
            });

            group.MapGet("/{eventId:guid}", async (IEventAnalyticsRepository repo, Guid eventId) =>
            {
                var analytics = await repo.GetByEventIdAsync(eventId);
                return analytics.Count > 0 ? Results.Ok(analytics) : Results.NotFound();
            });
        }
    }
}
