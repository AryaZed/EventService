using EventService.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventAttendeeConfiguration : IEntityTypeConfiguration<EventAttendee>
{
    public void Configure(EntityTypeBuilder<EventAttendee> builder)
    {
        builder.HasKey(ea => new { ea.EventId, ea.UserId }); // ✅ Composite Primary Key

        builder.HasOne(ea => ea.Event)
            .WithMany(e => e.EventAttendees)
            .HasForeignKey(ea => ea.EventId)
            .OnDelete(DeleteBehavior.NoAction); // ✅ If event is deleted, remove attendees

        builder.HasOne(ea => ea.User)
            .WithMany(u => u.EventAttendees)
            .HasForeignKey(ea => ea.UserId)
            .OnDelete(DeleteBehavior.NoAction); // ✅ If user is deleted, remove their event records

        builder.Property(ea => ea.JoinedAt)
            .IsRequired();
    }
}
