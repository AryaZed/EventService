using EventService.Domain.Entities.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventAnalyticsConfiguration : IEntityTypeConfiguration<EventAnalytics>
{
    public void Configure(EntityTypeBuilder<EventAnalytics> builder)
    {
        builder.ToTable("EventAnalytics");

        builder.HasKey(ea => ea.Id);

        builder.Property(ea => ea.Id)
            .ValueGeneratedNever();

        builder.Property(ea => ea.ProcessedUsers)
            .IsRequired();

        builder.Property(ea => ea.SuccessCount)
            .IsRequired();

        builder.Property(ea => ea.FailureCount)
            .IsRequired();

        builder.Property(ea => ea.ProcessingDuration)
            .IsRequired();

        builder.Property(ea => ea.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(ea => ea.EngagementScore)
            .HasPrecision(5, 2) // Limits decimal precision
            .IsRequired();

        builder.HasOne(ea => ea.Event)
            .WithMany()
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
