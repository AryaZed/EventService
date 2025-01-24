using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence
{
    public static class ApplicationDbContextSeed
    {
        public static async Task SeedAsync(ApplicationDbContext context)
        {
            // ✅ Ensure Database is Migrated
            await context.Database.MigrateAsync();

            // ✅ Check if Subscription Plans Exist
            if (!await context.SubscriptionPlans.AnyAsync())
            {
                var freePlan = SubscriptionPlan.CreateGlobal("Free", maxEvents: 50, price: 0m, maxRequestsPerMinute: 10, maxRequestsPerHour: 500);
                var proPlan = SubscriptionPlan.CreateGlobal("Pro", maxEvents: 500, price: 29.99m, maxRequestsPerMinute: 50, maxRequestsPerHour: 5000);
                var enterprisePlan = SubscriptionPlan.CreateGlobal("Enterprise", maxEvents: 5000, price: 99.99m, maxRequestsPerMinute: 200, maxRequestsPerHour: 20000);

                await context.SubscriptionPlans.AddRangeAsync(freePlan, proPlan, enterprisePlan);
                await context.SaveChangesAsync();
            }

            // ✅ Check if Admin Business Exists
            if (!await context.Businesses.AnyAsync())
            {
                var proPlan = await context.SubscriptionPlans.FirstOrDefaultAsync(p => p.Name == "Pro");
                if (proPlan == null)
                {
                    return;
                }

                var adminBusiness = Business.Create(
                    name: "Admin Business",
                    contactEmail: "admin@eventservice.com",
                    phoneNumber: "+1234567890",
                    subscriptionPlan: proPlan
                );

                await context.Businesses.AddAsync(adminBusiness);
                await context.SaveChangesAsync();

                // ✅ Create Admin User for this Business
                var adminUser = User.Create(
                    name: "Admin User",
                    email: "admin@eventservice.com",
                    phoneNumber: "+1234567890",
                    business: adminBusiness
                );

                await context.Users.AddAsync(adminUser);
                await context.SaveChangesAsync();
            }
        }
    }
}
