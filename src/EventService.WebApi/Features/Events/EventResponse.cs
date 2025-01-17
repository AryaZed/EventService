using EventService.Domain.Entities.Events;

namespace EventService.WebApi.Features.Events;

public class EventResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime ScheduledAt { get; set; }
    public Guid BusinessId { get; set; }
    public string BusinessName { get; set; }
    public EventTargetRules TargetRules { get; set; } // Deserialize rules

    public static EventResponse FromEntity(Event eventEntity)
    {
        return new EventResponse
        {
            Id = eventEntity.Id,
            Title = eventEntity.Title,
            Description = eventEntity.Description,
            ScheduledAt = eventEntity.ScheduledAt,
            BusinessId = eventEntity.BusinessId,
            BusinessName = eventEntity.Business?.Name ?? "",
            TargetRules = eventEntity.GetTargetRules() // Deserialize JSON rules
        };
    }
}

