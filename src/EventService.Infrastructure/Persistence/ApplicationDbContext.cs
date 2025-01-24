using EventService.Domain.Entities.Businesses;
using EventService.Domain.Entities.Events;
using EventService.Domain.Entities.Users;
using EventService.Domain.Entities.Payments;
using EventService.Domain.Entities.AuditLogs;
using EventService.Domain.Entities.Integrations;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MassTransit.Courier.Contracts;
using EventService.Domain.Entities.Notifications;
using EventService.Domain.Entities.Analytics;

namespace EventService.Infrastructure.Persistence;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : DbContext(options)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public DbSet<Business> Businesses { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserGroup> UserGroups { get; set; }
    public DbSet<Role> Roles { get; set; } // ✅ Role-Based Access Control (RBAC)
    public DbSet<Event> Events { get; set; }
    public DbSet<EventRule> EventRules { get; set; } // ✅ Dynamic Targeting Rules
    public DbSet<Notification> Notifications { get; set; } // ✅ SMS, Email, Webhook
    public DbSet<Webhook> Webhooks { get; set; } // ✅ API Integrations
    public DbSet<SubscriptionPlan> SubscriptionPlans { get; set; } // ✅ Payment Plans
    public DbSet<Invoice> Invoices { get; set; } // ✅ Billing & Payment Tracking
    public DbSet<AuditLog> AuditLogs { get; set; } // ✅ Compliance & Security Logs
    public DbSet<EventAnalytics> EventAnalytics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // ✅ Multi-Tenancy Row-Level Filtering
        if (_httpContextAccessor.HttpContext?.Items["TenantId"] is string tenantId)
        {
            Guid parsedTenantId = Guid.Parse(tenantId);
            modelBuilder.Entity<User>().HasQueryFilter(u => u.BusinessId == parsedTenantId);
            modelBuilder.Entity<Event>().HasQueryFilter(e => e.BusinessId == parsedTenantId);
            modelBuilder.Entity<UserGroup>().HasQueryFilter(ug => ug.BusinessId == parsedTenantId);
            modelBuilder.Entity<Role>().HasQueryFilter(r => r.BusinessId == parsedTenantId);
            modelBuilder.Entity<Notification>().HasQueryFilter(n => n.BusinessId == parsedTenantId);
            modelBuilder.Entity<Webhook>().HasQueryFilter(w => w.BusinessId == parsedTenantId);
            modelBuilder.Entity<SubscriptionPlan>().HasQueryFilter(s => s.BusinessId == parsedTenantId);
            modelBuilder.Entity<Invoice>().HasQueryFilter(i => i.BusinessId == parsedTenantId);
        }
    }

    public async Task MigrateAndSeedAsync()
    {
        await Database.MigrateAsync();
        await ApplicationDbContextSeed.SeedAsync(this);
    }
}
