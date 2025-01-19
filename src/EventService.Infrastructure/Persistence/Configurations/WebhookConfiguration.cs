using EventService.Domain.Entities.Integrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Url)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(w => w.SecretKey)
               .IsRequired()
               .HasMaxLength(255);

        builder.Property(w => w.EventType)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasOne(w => w.Business)
               .WithMany(b => b.Webhooks)
               .HasForeignKey(w => w.BusinessId)
               .OnDelete(DeleteBehavior.Cascade); // If business is deleted, remove webhooks
    }
}
