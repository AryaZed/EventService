using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Events;

using System.Text.Json;
using System.Text.Json.Serialization;

public class Event
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public string Title { get; private set; }
    public string Description { get; private set; }
    public DateTime ScheduledAt { get; private set; }
    public Guid BusinessId { get; private set; }
    public Business Business { get; private set; }
    public string TargetRulesJson { get; private set; } // Stores dynamic conditions as JSON
    public List<EventRule> EventRules { get; private set; }

    private Event() { }

    [JsonConstructor]
    private Event(string title, string description, DateTime scheduledAt, Business business, string targetRulesJson)
    {
        Title = title;
        Description = description;
        ScheduledAt = scheduledAt;
        Business = business;
        BusinessId = business.Id;
        TargetRulesJson = targetRulesJson;
    }

    public static Event Create(string title, string description, DateTime scheduledAt, Business business, string targetRulesJson)
    {
        return new Event(title, description, scheduledAt, business, targetRulesJson);
    }

    // Deserialize rules into an object
    public EventTargetRules GetTargetRules()
    {
        return JsonSerializer.Deserialize<EventTargetRules>(TargetRulesJson) ?? new EventTargetRules();
    }
}
