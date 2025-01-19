using EventService.Application.Interfaces.Repositories;
using EventService.Domain.Entities.Payments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Application.Services.Businesses;

public class SubscriptionService
{
    private readonly IBusinessRepository _businessRepository;
    private readonly ISubscriptionPlanRepository _subscriptionPlanRepository;
    private readonly IInvoiceRepository _invoiceRepository;

    public SubscriptionService(
        IBusinessRepository businessRepository,
        ISubscriptionPlanRepository subscriptionPlanRepository,
        IInvoiceRepository invoiceRepository)
    {
        _businessRepository = businessRepository;
        _subscriptionPlanRepository = subscriptionPlanRepository;
        _invoiceRepository = invoiceRepository;
    }

    public async Task<bool> ChangeSubscriptionPlanAsync(Guid businessId, Guid newPlanId)
    {
        var business = await _businessRepository.GetByIdAsync(businessId);
        if (business == null) throw new Exception("Business not found");

        var newPlan = await _subscriptionPlanRepository.GetByIdAsync(newPlanId);
        if (newPlan == null) throw new Exception("Subscription plan not found");

        // ✅ Generate an invoice for the plan change
        var invoice = new Invoice(business.Id, newPlan.Id, newPlan.Price);
        await _invoiceRepository.AddAsync(invoice);

        // ✅ Apply the subscription change
        business.UpgradeSubscription(newPlan);
        await _businessRepository.UpdateAsync(business);

        return true;
    }

    public async Task<bool> CancelExpiredSubscriptionsAsync()
    {
        var expiredBusinesses = await _businessRepository.GetExpiredSubscriptionsAsync();

        foreach (var business in expiredBusinesses)
        {
            var defaultPlan = await _subscriptionPlanRepository.GetDefaultPlanAsync(); // Fallback Plan
            business.DowngradeSubscription(defaultPlan);
            await _businessRepository.UpdateAsync(business);
        }

        return true;
    }
}
