using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Events;

using EventService.Domain.Enums;
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
    public RecurrenceType? Recurrence { get; private set; }

    public List<EventAttendee> EventAttendees { get; private set; } = new List<EventAttendee>();

    public void SetRecurrence(RecurrenceType recurrence)
    {
        Recurrence = recurrence;
    }

    private Event() { }

    [JsonConstructor]
    private Event(string title, string description, DateTime scheduledAt, Business business, string targetRulesJson, RecurrenceType? recurrence)
    {
        Title = title;
        Description = description;
        ScheduledAt = scheduledAt;
        Business = business;
        BusinessId = business.Id;
        TargetRulesJson = targetRulesJson;
        Recurrence = recurrence;
    }

    public static Event Create(string title, string description, DateTime scheduledAt, Business business, string targetRulesJson, RecurrenceType? recurrence)
    {
        return new Event(title, description, scheduledAt, business, targetRulesJson, recurrence);
    }

    // Deserialize rules into an object
    public EventTargetRules GetTargetRules()
    {
        return JsonSerializer.Deserialize<EventTargetRules>(TargetRulesJson) ?? new EventTargetRules();
    }

    public DateTime? GetNextOccurrence()
    {
        return Recurrence switch
        {
            RecurrenceType.Daily => ScheduledAt.AddDays(1),
            RecurrenceType.Weekly => ScheduledAt.AddDays(7),
            RecurrenceType.Monthly => ScheduledAt.AddMonths(1),
            _ => null
        };
    }

    // ✅ New: Add attendee to event
    public void AddAttendee(User user)
    {
        if (!EventAttendees.Exists(a => a.UserId == user.Id))
        {
            EventAttendees.Add(new EventAttendee(this, user));
        }
    }

    // ✅ New: Remove attendee from event
    public void RemoveAttendee(User user)
    {
        var attendee = EventAttendees.Find(a => a.UserId == user.Id);
        if (attendee != null)
        {
            EventAttendees.Remove(attendee);
        }
    }
}


