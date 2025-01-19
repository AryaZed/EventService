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

        // ✅ If BusinessId is set, this SubscriptionPlan belongs to a specific Business
        builder.HasOne(sp => sp.Business)
               .WithOne(b => b.SubscriptionPlan)
               .HasForeignKey<SubscriptionPlan>(sp => sp.BusinessId)
               .OnDelete(DeleteBehavior.Cascade); // ✅ Delete SubscriptionPlan if Business is deleted
    }
}
