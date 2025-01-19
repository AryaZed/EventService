using EventService.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventRuleConfiguration : IEntityTypeConfiguration<EventRule>
{
    public void Configure(EntityTypeBuilder<EventRule> builder)
    {
        builder.HasKey(er => er.Id);

        builder.Property(er => er.RuleJson)
               .IsRequired();

        builder.HasOne(er => er.Event)
               .WithMany(e => e.EventRules)
               .HasForeignKey(er => er.EventId)
               .OnDelete(DeleteBehavior.Cascade); // Deleting event deletes rules
    }
}
