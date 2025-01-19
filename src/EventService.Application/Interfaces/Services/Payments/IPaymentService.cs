using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Interfaces.Services.Payments
{
    public interface IPaymentService
    {
        Task<string?> RequestPaymentAsync(decimal amount, string callbackUrl, string description);
        Task<bool> VerifyPaymentAsync(string authority, decimal amount);
    }
}
