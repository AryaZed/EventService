using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Domain.Entities.Payments;

public class Invoice
{
    public Guid Id { get; private set; } = Guid.CreateVersion7();
    public Guid BusinessId { get; private set; } // ✅ Multi-Tenancy
    public Guid SubscriptionId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = "USD";
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Pending;
    public DateTime IssuedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? PaidAt { get; private set; }

    private Invoice() { }

    public Invoice(Guid businessId, Guid subscriptionId, decimal amount, string currency = "USD")
    {
        BusinessId = businessId;
        SubscriptionId = subscriptionId;
        Amount = amount;
        Currency = currency;
    }

    public void MarkAsPaid()
    {
        Status = InvoiceStatus.Paid;
        PaidAt = DateTime.UtcNow;
    }
}

public enum InvoiceStatus
{
    Pending,
    Paid,
    Failed
}