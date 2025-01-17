namespace EventService.Domain.Entities.Events;

public class EventTargetRules
{
    public bool SendToAllUsers { get; set; } = false;
    public int? UserJoinedInLastDays { get; set; } // Example: Users who joined in the last 30 days
    public List<Guid>? SpecificUserIds { get; set; } // Target specific users
    public List<Guid>? TargetUserGroups { get; set; } // Target specific user groups
}