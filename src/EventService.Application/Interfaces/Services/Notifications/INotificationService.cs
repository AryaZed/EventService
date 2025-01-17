using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Services.Notifications;

public interface INotificationService
{
    Task SendSmsAsync(string phoneNumber, string message);
}
