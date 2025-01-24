using Carter;
using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Events;
using EventService.Domain.Enums;
using MassTransit;
using System.Text.Json;

namespace EventService.WebApi.Features.Events;

public class EventModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/events").WithTags("Events");

        group.MapPost("/", async (IPublishEndpoint publishEndpoint, EventRequest request) =>
        {
            var eventEntity = Domain.Entities.Events.Event.Create(request.Title, request.Description, request.ScheduledAt, Business.CreateExisting(request.BusinessId), "{}",request.Recurrence);
            await publishEndpoint.Publish(eventEntity);

            return Results.Accepted($"/api/events/{eventEntity.Id}", eventEntity);
        });

        group.MapGet("/", async (IEventRepository repo) =>
        {
            var events = await repo.GetAllAsync();
            return Results.Ok(events.Select(EventResponse.FromEntity));
        });

        group.MapGet("/{id:guid}", async (IEventRepository repo, Guid id) =>
        {
            var eventEntity = await repo.GetByIdAsync(id);
            return eventEntity is not null ? Results.Ok(EventResponse.FromEntity(eventEntity)) : Results.NotFound();
        });

        group.MapPut("/{id:guid}", async (IEventRepository repo, Guid id, EventRequest request) =>
        {
            var existingEvent = await repo.GetByIdAsync(id);
            if (existingEvent is null) return Results.NotFound();

            string targetRulesJson = JsonSerializer.Serialize(request.TargetRules);
            existingEvent = Domain.Entities.Events.Event.Create(request.Title, request.Description, request.ScheduledAt, Business.CreateExisting(request.BusinessId), targetRulesJson, request.Recurrence);
            await repo.UpdateAsync(existingEvent);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (IEventRepository repo, Guid id) =>
        {
            await repo.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}