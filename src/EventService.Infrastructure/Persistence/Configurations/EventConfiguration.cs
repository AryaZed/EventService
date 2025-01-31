using EventService.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title).IsRequired().HasMaxLength(255);
        builder.Property(e => e.Description).IsRequired();
        builder.Property(e => e.ScheduledAt).IsRequired();
        builder.Property(e => e.TargetRulesJson).IsRequired().HasColumnType("nvarchar(max)");

        builder.HasOne(e => e.Business)
            .WithMany()
            .HasForeignKey(e => e.BusinessId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.EventAttendees)
           .WithOne(ea => ea.Event)
           .HasForeignKey(ea => ea.EventId)
           .OnDelete(DeleteBehavior.NoAction);
    }
}