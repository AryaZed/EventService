using EventService.Domain.Entities.Events;

namespace EventService.WebApi.Features.Events;

public class EventRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTime ScheduledAt { get; set; }
    public required Guid BusinessId { get; set; }
    public required EventTargetRules TargetRules { get; set; } // Accept dynamic rules
}
