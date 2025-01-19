using EventService.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Recipient).IsRequired().HasMaxLength(255);
        builder.Property(n => n.Message).IsRequired();
        builder.Property(n => n.Type).IsRequired();
        builder.Property(n => n.Status).IsRequired();
    }
}
