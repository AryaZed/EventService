using EventService.Domain.Entities.AuditLogs;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventService.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Action).IsRequired().HasMaxLength(255);
        builder.Property(a => a.UserId).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Entity).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Changes).IsRequired();
    }
}
