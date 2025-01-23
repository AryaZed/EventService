using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Integrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Services.Integrations
{
    public interface IWebhookService
    {
        Task TriggerWebhooksAsync(Event eventEntity);
        Task<bool> SendWebhookAsync(Guid webhookId, object data);
    }
}
