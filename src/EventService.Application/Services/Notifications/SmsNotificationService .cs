using EventService.Application.Interfaces.Services.Notifications;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Services.Notifications;

public class SmsNotificationService : INotificationService
{
    private readonly ILogger<SmsNotificationService> _logger;

    public SmsNotificationService(ILogger<SmsNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task SendSmsAsync(string phoneNumber, string message)
    {
        // Simulated delay
        await Task.Delay(100);

        _logger.LogInformation("📩 Sent SMS to {PhoneNumber}: {Message}", phoneNumber, message);
    }
}
