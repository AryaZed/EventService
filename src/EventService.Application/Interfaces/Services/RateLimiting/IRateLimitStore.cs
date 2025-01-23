using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Services.RateLimiting
{
    public interface IRateLimitStore
    {
        Task<bool> IsAllowedAsync(Guid tenantId, int maxPerMinute, int maxPerHour);
    }
}
