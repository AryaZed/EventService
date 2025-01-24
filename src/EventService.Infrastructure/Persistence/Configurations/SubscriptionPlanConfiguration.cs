using EventService.Domain.Entities.Businesses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.HasKey(sp => sp.Id);

        builder.Property(sp => sp.Name)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(sp => sp.MaxEventsPerMonth)
               .IsRequired();

        builder.Property(sp => sp.Price)
               .IsRequired()
               .HasColumnType("decimal(18,2)");

        builder.Property(sp => sp.MaxRequestsPerMinute)
               .IsRequired();

        builder.Property(sp => sp.MaxRequestsPerHour)
               .IsRequired();

        builder.HasOne(sp => sp.Business)
                 .WithMany(b => b.CreatedSubscriptionPlans)
                 .HasForeignKey(sp => sp.BusinessId)
                 .OnDelete(DeleteBehavior.Restrict); // ✅ Prevents cascading delete
    }
}
