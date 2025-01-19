using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Notifications;

public class Notification
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BusinessId { get; private set; } // ✅ Multi-Tenancy
    public Guid EventId { get; private set; }
    public string Recipient { get; private set; } // Email or Phone Number
    public string Message { get; private set; }
    public NotificationType Type { get; private set; }
    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Notification() { }

    public Notification(Guid businessId, Guid eventId, string recipient, string message, NotificationType type)
    {
        BusinessId = businessId;
        EventId = eventId;
        Recipient = recipient;
        Message = message;
        Type = type;
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
    }

    public void MarkAsFailed()
    {
        Status = NotificationStatus.Failed;
    }
}

public enum NotificationType
{
    SMS,
    Email,
    Webhook
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}