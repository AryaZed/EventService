using EventService.Domain.Entities.Events;
using EventService.Domain.Enums;

namespace EventService.WebApi.Features.Events;

public class EventRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required DateTime ScheduledAt { get; set; }
    public required Guid BusinessId { get; set; }
    public required EventTargetRules TargetRules { get; set; }
    public required RecurrenceType Recurrence { get; set; }
}
