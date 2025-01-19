namespace EventService.Domain.Entities.Events;

public class EventRule
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid EventId { get; private set; }
    public string RuleJson { get; private set; }
    public Event Event { get; set; }

    public EventRule(Guid eventId, string ruleJson)
    {
        EventId = eventId;
        RuleJson = ruleJson;
    }
}
