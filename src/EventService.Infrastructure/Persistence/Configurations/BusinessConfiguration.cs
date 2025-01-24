using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using EventService.Domain.Entities.Businesses;

namespace EventService.Infrastructure.Persistence.Configurations;

public class BusinessConfiguration : IEntityTypeConfiguration<Business>
{
    public void Configure(EntityTypeBuilder<Business> builder)
    {
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Name).IsRequired().HasMaxLength(255);
        builder.Property(b => b.ContactEmail).IsRequired().HasMaxLength(255);
        builder.Property(b => b.PhoneNumber).IsRequired().HasMaxLength(20);

        builder.HasMany(b => b.Users)
            .WithOne(u => u.Business)
            .HasForeignKey(u => u.BusinessId);

        builder.HasOne(b => b.SubscriptionPlan)
                  .WithMany() // No inverse navigation property
                  .HasForeignKey(b => b.SubscriptionPlanId)
                  .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents cascading delete

        builder.HasMany(b => b.CreatedSubscriptionPlans)
               .WithOne(sp => sp.Business)
               .HasForeignKey(sp => sp.BusinessId)
               .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents accidental deletions
    }
}
