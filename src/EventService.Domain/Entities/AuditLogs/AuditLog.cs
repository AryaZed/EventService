using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.AuditLogs;

public class AuditLog
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BusinessId { get; private set; } // ✅ Multi-Tenancy
    public string Action { get; private set; }
    public string UserId { get; private set; }
    public string Entity { get; private set; } // e.g., "User", "Event"
    public string Changes { get; private set; }
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow;

    private AuditLog() { }

    public AuditLog(Guid businessId, string action, string userId, string entity, string changes)
    {
        BusinessId = businessId;
        Action = action;
        UserId = userId;
        Entity = entity;
        Changes = changes;
    }
}