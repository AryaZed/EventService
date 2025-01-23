using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Models.Integrations
{
    public class WebhookRetryPayload
    {
        public Guid WebhookId { get; set; } // ✅ Webhook Identifier
        public object Payload { get; set; } // ✅ Event Data to Send

        public WebhookRetryPayload(Guid webhookId, object payload)
        {
            WebhookId = webhookId;
            Payload = payload;
        }
    }
}
